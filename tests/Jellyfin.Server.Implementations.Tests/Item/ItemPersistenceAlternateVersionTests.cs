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
using DbLinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

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

    private static Movie CreateMovie(Guid id, string path) => new()
    {
        Id = id,
        Name = "Movie",
        Path = path
    };
}
