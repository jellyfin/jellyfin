using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LibraryTaskScheduler;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

/// <summary>
/// A scan saves the new children of a folder without the ones the database refuses. Such an item does not exist, so
/// the rest of the scan must not hand the alternate versions of another video to it, demote a video below it, or
/// refresh it.
/// </summary>
[Collection("LibraryManagerTests")]
public sealed class FolderItemsNotSavedTests : IDisposable
{
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<IItemRepository> _itemRepository = new();
    private readonly Folder _folder = new() { Id = Guid.NewGuid(), Path = "/movies" };
    private readonly RecordingScheduler _scheduler = new();
    private readonly List<BaseItem> _updated = [];
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IItemRepository? _previousItemRepository;
    private readonly IMediaSourceManager? _previousMediaSourceManager;
    private readonly IRecordingsManager? _previousRecordingsManager;
    private readonly ILimitedConcurrencyLibraryScheduler? _previousScheduler;
    private readonly Microsoft.Extensions.Logging.ILogger<BaseItem>? _previousLogger;

    public FolderItemsNotSavedTests()
    {
        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousItemRepository = BaseItem.ItemRepository;
        _previousMediaSourceManager = BaseItem.MediaSourceManager;
        _previousRecordingsManager = Video.RecordingsManager;
        _previousScheduler = Folder.LimitedConcurrencyLibraryScheduler;
        _previousLogger = BaseItem.Logger;

        var mediaSourceManager = new Mock<IMediaSourceManager>();
        mediaSourceManager.Setup(m => m.GetPathProtocol(It.IsAny<string>())).Returns(MediaProtocol.File);
        BaseItem.MediaSourceManager = mediaSourceManager.Object;
        Video.RecordingsManager = Mock.Of<IRecordingsManager>();
        Folder.LimitedConcurrencyLibraryScheduler = _scheduler;
        BaseItem.Logger = NullLogger<BaseItem>.Instance;

        _libraryManager
            .Setup(l => l.UpdateItemAsync(It.IsAny<BaseItem>(), It.IsAny<BaseItem>(), It.IsAny<ItemUpdateType>(), It.IsAny<CancellationToken>()))
            .Callback<BaseItem, BaseItem, ItemUpdateType, CancellationToken>((item, _, _, _) => _updated.Add(item))
            .Returns(Task.CompletedTask);
        _libraryManager.Setup(l => l.GetLocalAlternateVersionIds(It.IsAny<Video>())).Returns([]);
        _libraryManager.Setup(l => l.GetLinkedAlternateVersions(It.IsAny<Video>())).Returns([]);
        BaseItem.LibraryManager = _libraryManager.Object;
        BaseItem.ItemRepository = _itemRepository.Object;
    }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ItemRepository = _previousItemRepository!;
        BaseItem.MediaSourceManager = _previousMediaSourceManager!;
        Video.RecordingsManager = _previousRecordingsManager!;
        Folder.LimitedConcurrencyLibraryScheduler = _previousScheduler!;
        BaseItem.Logger = _previousLogger!;
    }

    [Fact]
    public async Task ValidateChildren_NewPrimaryNotSaved_DemotesNothingAndKeepsTheMergedVersionVisible()
    {
        // The library has primary D with E merged into it by hand. A new file C now takes D as its local alternate.
        var oldPrimary = CreateMovie("/movies/Movie/Movie - D.mkv");
        var merged = CreateMovie("/elsewhere/Movie - E.mkv");
        merged.SetPrimaryVersionId(oldPrimary.Id);
        var newPrimary = CreateMovie("/movies/Movie/Movie - C.mkv");
        newPrimary.LocalAlternateVersions = [oldPrimary.Path];
        oldPrimary.SetParent(_folder);
        Children(stored: [oldPrimary], resolved: [newPrimary]);
        _libraryManager.Setup(l => l.GetLinkedAlternateVersions(oldPrimary)).Returns([merged]);
        _libraryManager.Setup(l => l.GetItemById(merged.Id)).Returns(merged);
        NotSaved(newPrimary);

        await Validate();

        Assert.Empty(_updated);
        Assert.Null(oldPrimary.PrimaryVersionId);
        Assert.Equal(oldPrimary.Id, merged.PrimaryVersionId);
        _libraryManager.Verify(l => l.RerouteLinkedChildReferencesAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ValidateChildren_ReplacingPrimaryNotSaved_KeepsTheOldPrimaryAndItsAlternateVersion()
    {
        // Primary D owns alternate A. D is gone, and a new file C takes over A.
        var oldPrimary = CreateMovie("/movies/Movie/Movie - D.mkv");
        var alternate = CreateMovie("/movies/Movie/Movie - A.mkv");
        alternate.OwnerId = oldPrimary.Id;
        alternate.SetPrimaryVersionId(oldPrimary.Id);
        oldPrimary.LocalAlternateVersions = [alternate.Path];
        var newPrimary = CreateMovie("/movies/Movie/Movie - C.mkv");
        newPrimary.LocalAlternateVersions = [alternate.Path];
        oldPrimary.SetParent(_folder);
        alternate.SetParent(_folder);
        Children(stored: [oldPrimary, alternate], resolved: [newPrimary]);
        _libraryManager.Setup(l => l.GetLocalAlternateVersionIds(oldPrimary)).Returns([alternate.Id]);
        _libraryManager.Setup(l => l.GetItemById(alternate.Id)).Returns(alternate);
        NotSaved(newPrimary);

        await Validate();

        Assert.Empty(_updated);
        Assert.Equal(oldPrimary.Id, alternate.OwnerId);
        Assert.Equal(oldPrimary.Id, alternate.PrimaryVersionId);
        Assert.Equal(_folder.Id, oldPrimary.ParentId);
        _libraryManager.Verify(l => l.DeleteItem(oldPrimary, It.IsAny<DeleteOptions>(), It.IsAny<BaseItem>(), It.IsAny<bool>()), Times.Never);
        _libraryManager.Verify(l => l.RerouteLinkedChildReferencesAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ValidateChildren_ItemNotSaved_IsNotRefreshed()
    {
        var saved = CreateMovie("/movies/saved.mkv");
        var refused = CreateMovie("/movies/refused.mkv");
        Children(stored: [], resolved: [saved, refused]);
        NotSaved(refused);

        await Validate();

        Assert.Equal(new[] { saved.Id }, Assert.Single(_scheduler.Batches));
    }

    [Fact]
    public async Task ValidateChildren_LibraryManagerReportingNothing_RefreshesEveryNewItem()
    {
        // A library manager that does not implement the report, such as a loose mock, gives back nothing.
        var first = CreateMovie("/movies/first.mkv");
        var second = CreateMovie("/movies/second.mkv");
        Children(stored: [], resolved: [first, second]);

        await Validate();

        Assert.Equal(new[] { first.Id, second.Id }, Assert.Single(_scheduler.Batches));
    }

    private static Movie CreateMovie(string path) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Movie",
        Path = path
    };

    private Task Validate()
        => _folder.ValidateChildren(
            new Progress<double>(),
            new MetadataRefreshOptions(Mock.Of<IDirectoryService>()),
            recursive: false,
            cancellationToken: CancellationToken.None);

    private void Children(IReadOnlyList<BaseItem> stored, IReadOnlyList<BaseItem> resolved)
    {
        _itemRepository.Setup(r => r.GetItemList(It.IsAny<InternalItemsQuery>())).Returns(stored);
        _libraryManager
            .Setup(l => l.ResolvePaths(It.IsAny<IEnumerable<FileSystemMetadata>>(), It.IsAny<IDirectoryService>(), It.IsAny<Folder>(), It.IsAny<LibraryOptions>(), It.IsAny<CollectionType?>()))
            .Returns(resolved);
    }

    private void NotSaved(BaseItem item)
        => _libraryManager
            .Setup(l => l.CreateItemsSkippingFailures(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<BaseItem?>(), It.IsAny<CancellationToken>()))
            .Returns([item.Id]);

    /// <summary>
    /// Records what the scan hands on, without running it.
    /// </summary>
    private sealed class RecordingScheduler : ILimitedConcurrencyLibraryScheduler
    {
        public List<Guid[]> Batches { get; } = [];

        public Task Enqueue<T>(T[] data, Func<T, IProgress<double>, Task> worker, IProgress<double> progress, CancellationToken cancellationToken)
        {
            Batches.Add(data.OfType<BaseItem>().Select(i => i.Id).ToArray());
            return Task.CompletedTask;
        }
    }
}
