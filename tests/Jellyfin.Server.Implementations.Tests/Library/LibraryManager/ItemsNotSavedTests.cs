using System;
using System.Collections.Generic;
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
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.LibraryManager;

/// <summary>
/// An item that the database refused while the rest of its batch was saved does not exist, so the library manager
/// must neither cache it nor announce it, while the items saved with it are cached and announced as usual.
/// </summary>
[Collection("LibraryManagerTests")]
public sealed class ItemsNotSavedTests : IDisposable
{
    private readonly Emby.Server.Implementations.Library.LibraryManager _libraryManager;
    private readonly Movie _saved = CreateMovie();
    private readonly Movie _refused = CreateMovie();
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IItemRepository? _previousItemRepository;
    private readonly IRecordingsManager? _previousRecordingsManager;

    public ItemsNotSavedTests()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        fixture.Freeze<Mock<IServerConfigurationManager>>()
            .Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        fixture.Freeze<Mock<IItemPersistenceService>>()
            .Setup(p => p.SaveItemsSkippingFailures(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<CancellationToken>()))
            .Returns([_refused.Id]);

        // Nothing is stored, so an item is only found if it is cached.
        var itemRepository = fixture.Freeze<Mock<IItemRepository>>();
        itemRepository.Setup(i => i.RetrieveItem(It.IsAny<Guid>())).Returns((BaseItem)null!);

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
        _previousItemRepository = BaseItem.ItemRepository;
        _previousRecordingsManager = Video.RecordingsManager;
        BaseItem.LibraryManager = _libraryManager;
        BaseItem.ItemRepository = itemRepository.Object;
        BaseItem.ConfigurationManager ??= fixture.Create<IServerConfigurationManager>();

        // A video counts as a library item, which is what gets announced, unless it is being recorded.
        Video.RecordingsManager = Mock.Of<IRecordingsManager>();
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ItemRepository = _previousItemRepository!;
        Video.RecordingsManager = _previousRecordingsManager!;
    }

    [Fact]
    public void CreateItems_ItemNotSaved_IsNeitherCachedNorAnnounced()
    {
        var added = new List<Guid>();
        _libraryManager.ItemAdded += (_, e) => added.Add(e.Item.Id);

        _libraryManager.CreateItems([_saved, _refused], null, CancellationToken.None);

        Assert.Equal(new[] { _saved.Id }, added);
        Assert.Same(_saved, _libraryManager.GetItemById(_saved.Id));
        Assert.Null(_libraryManager.GetItemById(_refused.Id));
    }

    [Fact]
    public async Task UpdateItemsAsync_ItemNotSaved_IsNotAnnounced()
    {
        var updated = new List<Guid>();
        _libraryManager.ItemUpdated += (_, e) => updated.Add(e.Item.Id);

        await _libraryManager.UpdateItemsAsync([_saved, _refused], null!, ItemUpdateType.MetadataEdit, CancellationToken.None);

        Assert.Equal(new[] { _saved.Id }, updated);
    }

    private static Movie CreateMovie() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Movie"
    };
}
