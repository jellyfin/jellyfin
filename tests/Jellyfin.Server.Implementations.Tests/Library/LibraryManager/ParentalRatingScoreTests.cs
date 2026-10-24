using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.LibraryManager;

[Collection("LibraryManagerTests")]
public sealed class ParentalRatingScoreTests : IDisposable
{
    private readonly Emby.Server.Implementations.Library.LibraryManager _libraryManager;
    private readonly List<BaseItem> _saved = [];
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly ILocalizationManager? _previousLocalizationManager;

    public ParentalRatingScoreTests()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        fixture.Freeze<Mock<IServerConfigurationManager>>()
            .Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        fixture.Freeze<Mock<IItemPersistenceService>>()
            .Setup(p => p.SaveItems(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<BaseItem>, CancellationToken>((items, _) => _saved.AddRange(items));

        _libraryManager = fixture.Build<Emby.Server.Implementations.Library.LibraryManager>()
            .Do(s => s.AddParts(
                fixture.Create<IEnumerable<IResolverIgnoreRule>>(),
                [],
                fixture.Create<IEnumerable<IIntroProvider>>(),
                fixture.Create<IEnumerable<IBaseItemComparer>>(),
                fixture.Create<IEnumerable<ILibraryPostScanTask>>()))
            .Create();

        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousLocalizationManager = BaseItem.LocalizationManager;
        BaseItem.LibraryManager = _libraryManager;

        var localizationManagerMock = new Mock<ILocalizationManager>();
        localizationManagerMock.Setup(l => l.GetRatingScore("G", It.IsAny<string>())).Returns(new ParentalRatingScore(0, null));
        localizationManagerMock.Setup(l => l.GetRatingScore("XXX", It.IsAny<string>())).Returns(new ParentalRatingScore(1000, null));
        BaseItem.LocalizationManager = localizationManagerMock.Object;
        Video.RecordingsManager ??= fixture.Create<IRecordingsManager>();
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.LocalizationManager = _previousLocalizationManager!;
    }

    private static Movie CreateRatedMovie() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Up",
        PreferredMetadataCountryCode = "us",
        OfficialRating = "G",
        InheritedParentalRatingValue = 0
    };

    [Fact]
    public async Task UpdateItemAsync_ChangedCustomRating_StoresItsScore()
    {
        var movie = CreateRatedMovie();
        movie.CustomRating = "XXX";

        await _libraryManager.UpdateItemAsync(movie, null!, ItemUpdateType.MetadataEdit, CancellationToken.None);

        Assert.Equal(1000, Assert.Single(_saved).InheritedParentalRatingValue);
    }

    [Fact]
    public async Task UpdateItemAsync_ClearedRatings_ClearsScore()
    {
        var movie = CreateRatedMovie();
        movie.OfficialRating = null;

        await _libraryManager.UpdateItemAsync(movie, null!, ItemUpdateType.MetadataEdit, CancellationToken.None);

        Assert.Null(Assert.Single(_saved).InheritedParentalRatingValue);
    }

    [Fact]
    public void CreateItem_RatedItem_StoresItsScore()
    {
        var movie = CreateRatedMovie();
        movie.InheritedParentalRatingValue = null;
        movie.CustomRating = "XXX";

        _libraryManager.CreateItem(movie, null);

        Assert.Equal(1000, _saved.Single().InheritedParentalRatingValue);
    }
}
