using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemEntity = Jellyfin.Database.Implementations.Entities.BaseItemEntity;
using DbLinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;
using LinkedChildEntity = Jellyfin.Database.Implementations.Entities.LinkedChildEntity;
using MediaSegment = Jellyfin.Database.Implementations.Entities.MediaSegment;
using MediaSegmentType = Jellyfin.Database.Implementations.Enums.MediaSegmentType;
using TrickplayInfo = Jellyfin.Database.Implementations.Entities.TrickplayInfo;
using User = Jellyfin.Database.Implementations.Entities.User;
using UserData = Jellyfin.Database.Implementations.Entities.UserData;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the invariant that a video linked as an alternate version also carries the
/// PrimaryVersionId the item queries hide it by, including when it was already a library
/// item in its own right before it became a version.
/// </summary>
public sealed class ItemPersistenceAlternateVersionTests : SqliteDbTestFixture
{
    private const string PrimaryPath = "/movies/Movie/Movie - 4K.mkv";
    private const string VersionPath = "/movies/Movie/Movie - 1080p.mkv";

    private readonly ItemPersistenceService _service;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;

    public ItemPersistenceAlternateVersionTests()
    {
        // BaseItem resolves these through process-wide statics; restored in Dispose.
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousRecordingsManager = Video.RecordingsManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>()))
            .Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;

        // Video.SourceType asks this whether the file is an in-progress recording.
        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        // Paths round-trip through the host's virtual path mapping on the way in and out.
        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ReverseVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>())).Returns((string p) => p);

        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            appHost.Object,
            NullLogger<ItemPersistenceService>.Instance);
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        Video.RecordingsManager = _previousRecordingsManager!;
        base.Dispose(disposing);
    }

    [Fact]
    public void SaveItems_LocalAlternateVersionAlreadyAnItem_SetsPrimaryVersionId()
    {
        var primaryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var versionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        // The version was scanned as a standalone movie before it became a version, so it has a
        // presentation key of its own and no PrimaryVersionId.
        var version = CreateMovie(versionId, VersionPath);
        version.PresentationUniqueKey = "standalone";
        _service.SaveItems([version], CancellationToken.None);

        using (var ctx = CreateDbContext())
        {
            Assert.Null(ctx.BaseItems.First(e => e.Id.Equals(versionId)).PrimaryVersionId);
        }

        // Now the scan folds it into a primary, which is the item that gets saved.
        var primary = CreateMovie(primaryId, PrimaryPath);
        primary.LocalAlternateVersions = [VersionPath];
        _service.SaveItems([primary], CancellationToken.None);

        using (var ctx = CreateDbContext())
        {
            var link = Assert.Single(ctx.LinkedChildren.Where(e => e.ParentId.Equals(primaryId)));
            Assert.Equal(DbLinkedChildType.LocalAlternateVersion, link.ChildType);
            Assert.Equal(versionId, link.ChildId);

            var stored = ctx.BaseItems.First(e => e.Id.Equals(versionId));
            Assert.Equal(primaryId, stored.PrimaryVersionId);

            // Presentation-key grouping has to collapse it onto the primary as well.
            Assert.Equal(primaryId.ToString("N", CultureInfo.InvariantCulture), stored.PresentationUniqueKey);
        }
    }

    [Fact]
    public void SaveItems_LinkedAlternateVersionAlreadyAnItem_SetsPrimaryVersionId()
    {
        var primaryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var versionId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        _service.SaveItems([CreateMovie(versionId, VersionPath)], CancellationToken.None);

        var primary = CreateMovie(primaryId, PrimaryPath);
        primary.LinkedAlternateVersions =
        [
            new LinkedChild { ItemId = versionId, Type = LinkedChildType.LinkedAlternateVersion }
        ];
        _service.SaveItems([primary], CancellationToken.None);

        using var ctx = CreateDbContext();
        var link = Assert.Single(ctx.LinkedChildren.Where(e => e.ParentId.Equals(primaryId)));
        Assert.Equal(DbLinkedChildType.LinkedAlternateVersion, link.ChildType);
        Assert.Equal(primaryId, ctx.BaseItems.First(e => e.Id.Equals(versionId)).PrimaryVersionId);
    }

    [Fact]
    public void SaveItems_VersionAlreadyPointingAtPrimary_LeavesItAlone()
    {
        var primaryId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var versionId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        var version = CreateMovie(versionId, VersionPath);
        version.SetPrimaryVersionId(primaryId);
        _service.SaveItems([version], CancellationToken.None);

        var primary = CreateMovie(primaryId, PrimaryPath);
        primary.LocalAlternateVersions = [VersionPath];
        _service.SaveItems([primary], CancellationToken.None);

        using var ctx = CreateDbContext();
        var stored = ctx.BaseItems.First(e => e.Id.Equals(versionId));
        Assert.Equal(primaryId, stored.PrimaryVersionId);
        Assert.Equal(primaryId.ToString("N", CultureInfo.InvariantCulture), stored.PresentationUniqueKey);
    }

    [Fact]
    public void SaveItems_VideoListedAmongItsOwnVersions_KeepsItsOwnPrimaryVersionId()
    {
        var primaryId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var primary = CreateMovie(primaryId, PrimaryPath);
        primary.LocalAlternateVersions = [PrimaryPath];
        _service.SaveItems([primary], CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Null(ctx.BaseItems.First(e => e.Id.Equals(primaryId)).PrimaryVersionId);
    }

    [Fact]
    public void SaveItems_PromotedVersionStillPointingAtOldPrimary_DoesNotCreateACycle()
    {
        var promotedId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var oldPrimaryId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        _service.SaveItems([CreateMovie(oldPrimaryId, VersionPath)], CancellationToken.None);

        // The rescan resolves this one as the primary of the group, but it still carries the pointer
        // to the version it was promoted over.
        var promoted = CreateMovie(promotedId, PrimaryPath);
        promoted.SetPrimaryVersionId(oldPrimaryId);
        promoted.LocalAlternateVersions = [VersionPath];
        _service.SaveItems([promoted], CancellationToken.None);

        using var ctx = CreateDbContext();

        // Pointing the old primary back would hide both, and with them the whole group.
        Assert.Null(ctx.BaseItems.First(e => e.Id.Equals(oldPrimaryId)).PrimaryVersionId);
        Assert.Equal(oldPrimaryId, ctx.BaseItems.First(e => e.Id.Equals(promotedId)).PrimaryVersionId);
    }

    [Fact]
    public void SaveItems_OwnedVersionDisappeared_DeletesItThroughTheDeletePath()
    {
        var primaryId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var versionId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var userId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        SeedOwnedVersion(primaryId, versionId, userId);

        // The rescan no longer finds the second file, so the version it owned is gone with it.
        _service.SaveItems([CreateMovie(primaryId, PrimaryPath)], CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Empty(ctx.BaseItems.Where(e => e.Id.Equals(versionId)));
        Assert.Empty(ctx.TrickplayInfos.Where(e => e.ItemId.Equals(versionId)));
        Assert.Empty(ctx.MediaSegments.Where(e => e.ItemId.Equals(versionId)));

        // Dropping the row alone would take the play state with it; the delete path parks it on the
        // placeholder instead, which is the difference between this and a plain RemoveRange.
        var userData = Assert.Single(ctx.UserData.Where(e => e.UserId.Equals(userId)));
        Assert.Equal(BaseItemRepository.PlaceholderId, userData.ItemId);
        Assert.NotNull(userData.RetentionDate);
    }

    private void SeedOwnedVersion(Guid primaryId, Guid versionId, Guid userId)
    {
        using var context = CreateDbContext();

        context.BaseItems.Add(NewItem(primaryId, PrimaryPath, null));
        context.BaseItems.Add(NewItem(versionId, VersionPath, primaryId));
        context.LinkedChildren.Add(new LinkedChildEntity
        {
            ParentId = primaryId,
            ChildId = versionId,
            ChildType = DbLinkedChildType.LocalAlternateVersion,
            SortOrder = 0
        });
        context.TrickplayInfos.Add(new TrickplayInfo
        {
            ItemId = versionId,
            Width = 320,
            Height = 180,
            TileWidth = 10,
            TileHeight = 10,
            ThumbnailCount = 1,
            Interval = 10000,
            Bandwidth = 1
        });
        context.MediaSegments.Add(new MediaSegment
        {
            Id = Guid.NewGuid(),
            ItemId = versionId,
            Type = MediaSegmentType.Intro,
            StartTicks = 0,
            EndTicks = 1000,
            SegmentProviderId = "Test"
        });
        context.Users.Add(new User("version-watcher", "Default", "Default")
        {
            Id = userId
        });
        context.SaveChanges();

        context.UserData.Add(new UserData
        {
            ItemId = versionId,
            UserId = userId,
            CustomDataKey = "key",
            Played = true,
            Item = null,
            User = null
        });
        context.SaveChanges();
    }

    private static BaseItemEntity NewItem(Guid id, string? path, Guid? ownerId) => new()
    {
        Id = id,
        Type = "MediaBrowser.Controller.Entities.Movies.Movie",
        Name = "Movie",
        Path = path,
        OwnerId = ownerId,
        PresentationUniqueKey = id.ToString("N", CultureInfo.InvariantCulture)
    };

    private static Movie CreateMovie(Guid id, string path) => new()
    {
        Id = id,
        Name = "Movie",
        Path = path
    };
}
