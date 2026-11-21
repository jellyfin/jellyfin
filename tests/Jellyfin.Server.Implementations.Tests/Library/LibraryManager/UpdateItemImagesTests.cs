using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Entities;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.LibraryManager;

[Collection("LibraryManagerTests")]
public sealed class UpdateItemImagesTests : IDisposable
{
    private readonly Emby.Server.Implementations.Library.LibraryManager _libraryManager;
    private readonly Mock<IItemPersistenceService> _persistenceServiceMock;
    private readonly string _imagePath = Path.GetTempFileName();
    private readonly ILibraryManager? _previousLibraryManager;

    public UpdateItemImagesTests()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        fixture.Freeze<Mock<IServerConfigurationManager>>()
            .Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        fixture.Freeze<Mock<IImageProcessor>>()
            .Setup(p => p.GetImageDimensions(It.IsAny<BaseItem>(), It.IsAny<ItemImageInfo>()))
            .Returns(new ImageDimensions(1000, 1500));
        _persistenceServiceMock = fixture.Freeze<Mock<IItemPersistenceService>>();

        _libraryManager = fixture.Build<Emby.Server.Implementations.Library.LibraryManager>()
            .Do(s => s.AddParts(
                fixture.Create<IEnumerable<IResolverIgnoreRule>>(),
                [],
                fixture.Create<IEnumerable<IIntroProvider>>(),
                fixture.Create<IEnumerable<IBaseItemComparer>>(),
                fixture.Create<IEnumerable<ILibraryPostScanTask>>()))
            .Create();

        // BaseItem resolves this through a process-wide static; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        BaseItem.LibraryManager = _libraryManager;
        Video.RecordingsManager ??= fixture.Create<IRecordingsManager>();
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        File.Delete(_imagePath);
    }

    private Movie CreateMovieWithOutdatedImage()
    {
        var movie = new Movie { Id = Guid.NewGuid(), Name = "White Christmas" };
        movie.SetImage(new ItemImageInfo { Path = _imagePath, Type = ImageType.Primary }, 0);
        return movie;
    }

    [Fact]
    public async Task UpdateItemAsync_OutdatedImage_SavesItWithTheItemOnly()
    {
        var movie = CreateMovieWithOutdatedImage();

        await _libraryManager.UpdateItemAsync(movie, null!, ItemUpdateType.ImageUpdate, CancellationToken.None);

        Assert.Equal(1000, movie.GetImageInfo(ImageType.Primary, 0)!.Width);
        _persistenceServiceMock.Verify(p => p.SaveImagesAsync(It.IsAny<BaseItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _persistenceServiceMock.Verify(p => p.SaveItems(It.Is<IReadOnlyList<BaseItem>>(items => items.Contains(movie)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateImagesAsync_OutdatedImage_SavesIt()
    {
        var movie = CreateMovieWithOutdatedImage();

        await _libraryManager.UpdateImagesAsync(movie);

        Assert.Equal(1000, movie.GetImageInfo(ImageType.Primary, 0)!.Width);
        _persistenceServiceMock.Verify(p => p.SaveImagesAsync(movie, It.IsAny<CancellationToken>()), Times.Once);
    }
}
