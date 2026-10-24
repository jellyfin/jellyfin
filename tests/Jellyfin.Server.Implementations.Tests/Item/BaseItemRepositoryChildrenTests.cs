using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the children query the library scan runs against a folder: a version merged by hand is
/// hidden from ordinary queries, but the scan has to see it or it takes the row for a new item and
/// recreates it, splitting the version group apart again.
/// </summary>
public sealed class BaseItemRepositoryChildrenTests : SqliteDbTestFixture
{
    private static readonly Guid _folderId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _primaryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid _mergedVersionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid _ownedVersionId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private readonly BaseItemRepository _repository;

    public BaseItemRepositoryChildrenTests()
    {
        var itemTypeLookup = new ItemTypeLookup();
        _repository = CreateBaseItemRepository(itemTypeLookup);

        var movieTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];
        using var ctx = CreateDbContext();
        ctx.BaseItems.Add(new BaseItemEntity
        {
            Id = _folderId,
            Type = itemTypeLookup.BaseItemKindNames[BaseItemKind.Folder]!,
            Name = "Movies",
            Path = "/movies",
            IsFolder = true
        });
        ctx.BaseItems.Add(CreateMovie(_primaryId, movieTypeName!, "Big Buck Bunny", "/media1/Big Buck Bunny/bbb-1080p.mp4", null, null));
        ctx.BaseItems.Add(CreateMovie(_mergedVersionId, movieTypeName!, "Big Buck Bunny", "/media2/Big Buck Bunny/bbb-2160p.mp4", _primaryId, null));
        ctx.BaseItems.Add(CreateMovie(_ownedVersionId, movieTypeName!, "Big Buck Bunny - 720p", "/media1/Big Buck Bunny/bbb-720p.mp4", _primaryId, _primaryId));
        ctx.SaveChanges();
    }

    [Fact]
    public void GetItemList_ChildrenOfFolder_ExcludesAlternateVersionsByDefault()
    {
        var result = _repository.GetItemList(new InternalItemsQuery { ParentId = _folderId });

        var item = Assert.Single(result);
        Assert.Equal(_primaryId, item.Id);
    }

    [Fact]
    public void GetItemList_ChildrenOfFolderIncludingAlternateVersions_KeepsMergedVersion()
    {
        var result = _repository.GetItemList(new InternalItemsQuery
        {
            ParentId = _folderId,
            IncludeAlternateVersions = true
        });

        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.Id.Equals(_primaryId));
        Assert.Contains(result, i => i.Id.Equals(_mergedVersionId));
    }

    [Fact]
    public void GetItemList_ChildrenOfFolderIncludingAlternateVersions_StillExcludesOwnedVersion()
    {
        // A version stored next to the file it belongs to is owned by its primary and is never
        // resolved on its own, so the scan must not see it as a child of the folder either.
        var result = _repository.GetItemList(new InternalItemsQuery
        {
            ParentId = _folderId,
            IncludeAlternateVersions = true
        });

        Assert.DoesNotContain(result, i => i.Id.Equals(_ownedVersionId));
    }

    private static BaseItemEntity CreateMovie(Guid id, string typeName, string name, string path, Guid? primaryVersionId, Guid? ownerId)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = typeName,
            Name = name,
            Path = path,
            ParentId = _folderId,
            TopParentId = _folderId,
            PresentationUniqueKey = (primaryVersionId ?? id).ToString("N"),
            PrimaryVersionId = primaryVersionId,
            OwnerId = ownerId,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }
}
