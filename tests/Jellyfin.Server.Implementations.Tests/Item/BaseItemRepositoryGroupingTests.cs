using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class BaseItemRepositoryGroupingTests : SqliteDbTestFixture
{
    private static readonly Guid _movieLibraryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _movie4KLibraryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    // Canonical order puts each lower id first, while the Guid.ToByteArray() byte order and the
    // uniqueidentifier order of SQL Server both put it last.
    private static readonly Guid _lowerId = Guid.Parse("0fffffff-0000-0000-0000-0000000000ff");
    private static readonly Guid _higherId = Guid.Parse("10000000-0000-0000-0000-000000000000");
    private static readonly Guid _alternateId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid _otherLowerId = Guid.Parse("0fffffff-0000-0000-0000-0000000000fe");
    private static readonly Guid _otherHigherId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private readonly BaseItemRepository _repository;
    private readonly string _movieTypeName;
    private readonly string _folderTypeName;
    private readonly string _episodeTypeName;

    public BaseItemRepositoryGroupingTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        _movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];
        _folderTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Folder];
        _episodeTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Episode];

        _repository = CreateBaseItemRepository(itemTypeLookup);
    }

    [Fact]
    public void GetItemList_VersionGroup_ReturnsPrimaryVersion()
    {
        // The alternate version sorts before both primaries by id, so a plain Min(Id) per
        // presentation key would wrongly pick the alternate as the group representative.
        var presentationKey = _lowerId.ToString("N");

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(_higherId, "Movie - 4K", presentationKey, null));
            ctx.BaseItems.Add(CreateMovieEntity(_lowerId, "Movie", presentationKey, null));
            ctx.BaseItems.Add(CreateMovieEntity(_alternateId, "Movie - 1080p", presentationKey, _lowerId));
            ctx.SaveChanges();
        }

        var result = _repository.GetItemList(CreateQuery());

        var item = Assert.Single(result);
        Assert.Equal(_lowerId, item.Id);
    }

    [Fact]
    public void GetItemList_GroupWithoutPrimary_FallsBackToMinId()
    {
        var firstId = _lowerId;
        var secondId = _higherId;
        var otherPrimaryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var presentationKey = otherPrimaryId.ToString("N");

        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateMovieEntity(firstId, "Movie", presentationKey, otherPrimaryId));
            ctx.BaseItems.Add(CreateMovieEntity(secondId, "Movie - 4K", presentationKey, otherPrimaryId));
            ctx.SaveChanges();
        }

        var result = _repository.GetItemList(CreateQuery());

        var item = Assert.Single(result);
        Assert.Equal(firstId, item.Id);
    }

    [Fact]
    public void GetItemList_GroupBySeriesPresentationKey_ReturnsLowestIdOfEachSeries()
    {
        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.Add(CreateEpisodeEntity(_higherId, "series-a"));
            ctx.BaseItems.Add(CreateEpisodeEntity(_lowerId, "series-a"));
            ctx.BaseItems.Add(CreateEpisodeEntity(_otherHigherId, "series-b"));
            ctx.BaseItems.Add(CreateEpisodeEntity(_otherLowerId, "series-b"));
            ctx.SaveChanges();
        }

        var result = _repository.GetItemList(new InternalItemsQuery(new Database.Implementations.Entities.User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Episode],
            GroupBySeriesPresentationUniqueKey = true
        });

        Assert.Equal(
            new[] { _lowerId, _otherLowerId }.Order(),
            result.Select(i => i.Id).Order());
    }

    [Fact]
    public void GetItemList_LibraryWithoutThePrimaryOfTheGroup_KeepsTheVersionVisible()
    {
        var primaryId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var versionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var sameLibraryPrimaryId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var sameLibraryVersionId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        SeedCrossLibraryGroup(primaryId, versionId, sameLibraryPrimaryId, sameLibraryVersionId);

        var result = _repository.GetItemList(CreateLibraryQuery(_movieLibraryId));

        // The version stands in for the group in the library it lives in, because its primary is in
        // a library of its own; a group merged inside this library still collapses onto its primary.
        Assert.Contains(result, i => i.Id.Equals(versionId));
        Assert.Contains(result, i => i.Id.Equals(sameLibraryPrimaryId));
        Assert.DoesNotContain(result, i => i.Id.Equals(sameLibraryVersionId));
        Assert.DoesNotContain(result, i => i.Id.Equals(primaryId));
    }

    [Fact]
    public void GetItemList_LibraryHoldingThePrimary_ReturnsThePrimary()
    {
        var primaryId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var versionId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        SeedCrossLibraryGroup(primaryId, versionId);

        var result = _repository.GetItemList(CreateLibraryQuery(_movie4KLibraryId));

        var item = Assert.Single(result);
        Assert.Equal(primaryId, item.Id);
    }

    [Fact]
    public void GetItemList_BothLibrariesOfACrossLibraryGroup_ReturnsItOnce()
    {
        var primaryId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var versionId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        SeedCrossLibraryGroup(primaryId, versionId);

        var result = _repository.GetItemList(CreateLibraryQuery(_movieLibraryId, _movie4KLibraryId));

        // With both libraries in scope the presentation key grouping collapses the version.
        var item = Assert.Single(result);
        Assert.Equal(primaryId, item.Id);
    }

    [Fact]
    public void GetItems_LibraryWithoutThePrimaryOfTheGroup_CountsWhatItLists()
    {
        var primaryId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var versionId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        SeedCrossLibraryGroup(primaryId, versionId);

        var listed = _repository.GetItemList(CreateLibraryQuery(_movieLibraryId)).Count;

        var query = CreateLibraryQuery(_movieLibraryId);
        query.EnableTotalRecordCount = true;
        query.Limit = 1;

        // The total the client pages against has to agree with the listing.
        Assert.Equal(1, listed);
        Assert.Equal(listed, _repository.GetItems(query).TotalRecordCount);
    }

    private static InternalItemsQuery CreateLibraryQuery(params Guid[] topParentIds)
    {
        return new InternalItemsQuery(new Database.Implementations.Entities.User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            TopParentIds = topParentIds
        };
    }

    private void SeedCrossLibraryGroup(
        Guid primaryId,
        Guid versionId,
        Guid? sameLibraryPrimaryId = null,
        Guid? sameLibraryVersionId = null)
    {
        using var ctx = CreateDbContext();
        ctx.BaseItems.Add(CreateFolderEntity(_movieLibraryId, "Movies"));
        ctx.BaseItems.Add(CreateFolderEntity(_movie4KLibraryId, "Movies-4K"));

        // The 4K version heads the group and lives in a library of its own.
        ctx.BaseItems.Add(CreateMovieEntity(primaryId, "Movie - 4K", primaryId.ToString("N"), null, _movie4KLibraryId));
        ctx.BaseItems.Add(CreateMovieEntity(versionId, "Movie", primaryId.ToString("N"), primaryId, _movieLibraryId));

        if (sameLibraryPrimaryId.HasValue && sameLibraryVersionId.HasValue)
        {
            ctx.BaseItems.Add(CreateMovieEntity(sameLibraryPrimaryId.Value, "Other - 4K", sameLibraryPrimaryId.Value.ToString("N"), null, _movieLibraryId));
            ctx.BaseItems.Add(CreateMovieEntity(sameLibraryVersionId.Value, "Other", sameLibraryPrimaryId.Value.ToString("N"), sameLibraryPrimaryId.Value, _movieLibraryId));
        }

        ctx.SaveChanges();
    }

    private BaseItemEntity CreateFolderEntity(Guid id, string name)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _folderTypeName,
            Name = name,
            Path = "/" + name,
            IsFolder = true
        };
    }

    private static InternalItemsQuery CreateQuery()
    {
        // IncludeOwnedItems keeps the alternate version rows in the query so the
        // grouping collapse is what picks the group representative.
        return new InternalItemsQuery(new Database.Implementations.Entities.User("test", "auth", "reset"))
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            IncludeOwnedItems = true
        };
    }

    private BaseItemEntity CreateEpisodeEntity(Guid id, string seriesPresentationKey)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _episodeTypeName,
            Name = "Episode",
            PresentationUniqueKey = id.ToString("N"),
            SeriesPresentationUniqueKey = seriesPresentationKey,
            MediaType = "Video",
            IsFolder = false,
            IsVirtualItem = false
        };
    }

    private BaseItemEntity CreateMovieEntity(Guid id, string name, string presentationKey, Guid? primaryVersionId, Guid? libraryId = null)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = _movieTypeName,
            Name = name,
            ParentId = libraryId,
            TopParentId = libraryId,
            PresentationUniqueKey = presentationKey,
            PrimaryVersionId = primaryVersionId,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }
}
