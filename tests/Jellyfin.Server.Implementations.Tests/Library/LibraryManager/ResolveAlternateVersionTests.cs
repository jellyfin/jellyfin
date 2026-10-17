using System;
using System.Collections.Generic;
using System.Threading;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.LibraryManager;

public sealed class ResolveAlternateVersionTests : IDisposable
{
    private const string PrimaryPath = "/movies/Up/Up.mkv";
    private const string AlternatePath = "/movies/Up/Up - 1080p.mkv";

    private readonly Emby.Server.Implementations.Library.LibraryManager _libraryManager;
    private readonly Mock<IItemPersistenceService> _persistenceServiceMock;
    private readonly Folder _staleParent;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IMediaSourceManager? _previousMediaSourceManager;
    private readonly IItemRepository? _previousItemRepository;

    public ResolveAlternateVersionTests()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        fixture.Freeze<Mock<IServerConfigurationManager>>()
            .Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        _persistenceServiceMock = fixture.Freeze<Mock<IItemPersistenceService>>();
        var itemRepositoryMock = fixture.Freeze<Mock<IItemRepository>>();
        fixture.Freeze<Mock<IFileSystem>>()
            .Setup(f => f.GetFileInfo(It.IsAny<string>()))
            .Returns<string>(path => new FileSystemMetadata { FullName = path });

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
        _previousMediaSourceManager = BaseItem.MediaSourceManager;
        _previousItemRepository = BaseItem.ItemRepository;
        BaseItem.LibraryManager = _libraryManager;

        var mediaSourceManagerMock = new Mock<IMediaSourceManager>();
        mediaSourceManagerMock.Setup(m => m.GetMediaStreams(It.IsAny<Guid>())).Returns([]);
        mediaSourceManagerMock.Setup(m => m.GetMediaAttachments(It.IsAny<Guid>())).Returns([]);
        BaseItem.MediaSourceManager = mediaSourceManagerMock.Object;

        // A reloaded listing comes back empty, so a stale entry surviving is visible.
        itemRepositoryMock.Setup(i => i.GetItemList(It.IsAny<InternalItemsQuery>())).Returns([]);
        BaseItem.ItemRepository = itemRepositoryMock.Object;

        BaseItem.FileSystem ??= fixture.Create<IFileSystem>();
        BaseItem.MediaSegmentManager ??= fixture.Create<IMediaSegmentManager>();
        BaseItem.ConfigurationManager ??= fixture.Create<IServerConfigurationManager>();
        Video.RecordingsManager ??= fixture.Create<IRecordingsManager>();

        var primary = new Movie
        {
            Name = "Up",
            Path = PrimaryPath,
            LocalAlternateVersions = [AlternatePath],
            Id = _libraryManager.GetNewItemId(PrimaryPath, typeof(Movie))
        };

        _staleParent = new Folder
        {
            Name = "Up",
            Path = "/movies/Up",
            Id = _libraryManager.GetNewItemId("/movies/Up", typeof(Folder))
        };

        var staleAlternate = new Video
        {
            Name = "Up - 1080p",
            Path = AlternatePath,
            OwnerId = primary.Id,
            ParentId = _staleParent.Id,
            Id = _libraryManager.GetNewItemId(AlternatePath, typeof(Video))
        };
        staleAlternate.SetPrimaryVersionId(primary.Id);

        itemRepositoryMock
            .Setup(i => i.RetrieveItem(It.IsAny<Guid>()))
            .Returns<Guid>(id => id.Equals(primary.Id) ? primary
                : id.Equals(staleAlternate.Id) ? staleAlternate
                : id.Equals(_staleParent.Id) ? _staleParent
                : null!);

        StaleAlternateId = staleAlternate.Id;
    }

    private Guid StaleAlternateId { get; }

    public void Dispose()
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.MediaSourceManager = _previousMediaSourceManager!;
        BaseItem.ItemRepository = _previousItemRepository!;
    }

    [Fact]
    public void ResolveAlternateVersion_StaleWrongTypeItem_DropsRowWithoutResavingPrimary()
    {
        // The alternate is stored under the id of the generic Video type while its primary is a Movie.
        _libraryManager.ResolveAlternateVersion(AlternatePath, typeof(Movie), null, null);

        _persistenceServiceMock.Verify(
            p => p.DeleteItem(It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1 && ids[0].Equals(StaleAlternateId))),
            Times.Once);

        // Saving the primary is what re-enters this method before the stale row is gone.
        _persistenceServiceMock.Verify(
            p => p.SaveItems(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void ResolveAlternateVersion_StaleWrongTypeItem_DropsCachedParentListing()
    {
        _staleParent.Children = [new Video { Name = "Up - 1080p", Path = AlternatePath }];

        _libraryManager.ResolveAlternateVersion(AlternatePath, typeof(Movie), null, null);

        Assert.Empty(_staleParent.Children);
    }
}
