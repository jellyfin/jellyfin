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
/// Box set collapsing on a library listing without a user, the case where the query is not grouped by
/// presentation key: members of a box set are replaced by the box set, other items stay, a box set shows once
/// however many members it has, and a box set with no member in the listing does not show.
/// </summary>
public sealed class BaseItemRepositoryBoxSetCollapseTests : SqliteDbTestFixture
{
    private static readonly Guid _libraryId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _otherLibraryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid _firstMemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _secondMemberId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _looseMovieId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid _elsewhereMovieId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid _boxSetId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid _elsewhereBoxSetId = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private readonly BaseItemRepository _repository;
    private readonly ItemTypeLookup _itemTypeLookup = new();

    public BaseItemRepositoryBoxSetCollapseTests()
    {
        _repository = CreateBaseItemRepository(_itemTypeLookup);
        Seed();
    }

    [Fact]
    public void GetItemList_CollapseAllTypes_ReplacesMembersByTheirBoxSet()
    {
        var ids = _repository.GetItemList(CreateQuery()).Select(i => i.Id).ToList();

        Assert.Equal(new[] { _boxSetId, _looseMovieId }.OrderBy(i => i), ids.OrderBy(i => i));
    }

    [Fact]
    public void GetItemList_CollapseMovies_ReplacesMembersByTheirBoxSet()
    {
        var query = CreateQuery();
        query.CollapseBoxSetItemTypes = [BaseItemKind.Movie];

        var ids = _repository.GetItemList(query).Select(i => i.Id).ToList();

        Assert.Equal(new[] { _boxSetId, _looseMovieId }.OrderBy(i => i), ids.OrderBy(i => i));
    }

    [Fact]
    public void GetItems_Collapse_CountsWhatItLists()
    {
        var query = CreateQuery();
        query.EnableTotalRecordCount = true;
        query.Limit = 1;

        Assert.Equal(2, _repository.GetItems(query).TotalRecordCount);
    }

    [Fact]
    public void GetItemList_WithoutCollapse_ListsEveryMovie()
    {
        var query = CreateQuery();
        query.CollapseBoxSetItems = false;

        var ids = _repository.GetItemList(query).Select(i => i.Id).ToList();

        Assert.Equal(new[] { _firstMemberId, _secondMemberId, _looseMovieId }.OrderBy(i => i), ids.OrderBy(i => i));
    }

    private static InternalItemsQuery CreateQuery()
    {
        return new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            TopParentIds = [_libraryId],
            CollapseBoxSetItems = true
        };
    }

    private void Seed()
    {
        var movie = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie];
        var boxSet = _itemTypeLookup.BaseItemKindNames[BaseItemKind.BoxSet];
        var folder = _itemTypeLookup.BaseItemKindNames[BaseItemKind.Folder];

        using var ctx = CreateDbContext();
        ctx.BaseItems.Add(Entity(_libraryId, folder, "Movies", null, isFolder: true));
        ctx.BaseItems.Add(Entity(_otherLibraryId, folder, "Other", null, isFolder: true));
        ctx.BaseItems.Add(Entity(_firstMemberId, movie, "First member", _libraryId));
        ctx.BaseItems.Add(Entity(_secondMemberId, movie, "Second member", _libraryId));
        ctx.BaseItems.Add(Entity(_looseMovieId, movie, "Not in a box set", _libraryId));
        ctx.BaseItems.Add(Entity(_elsewhereMovieId, movie, "In another library", _otherLibraryId));
        ctx.BaseItems.Add(Entity(_boxSetId, boxSet, "Box set", null, isFolder: true));
        ctx.BaseItems.Add(Entity(_elsewhereBoxSetId, boxSet, "Box set of another library", null, isFolder: true));
        ctx.LinkedChildren.Add(Link(_boxSetId, _firstMemberId, 0));
        ctx.LinkedChildren.Add(Link(_boxSetId, _secondMemberId, 1));
        ctx.LinkedChildren.Add(Link(_elsewhereBoxSetId, _elsewhereMovieId, 0));
        ctx.SaveChanges();
    }

    private static BaseItemEntity Entity(Guid id, string type, string name, Guid? libraryId, bool isFolder = false)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = type,
            Name = name,
            SortName = name,
            ParentId = libraryId,
            TopParentId = libraryId,
            PresentationUniqueKey = id.ToString("N"),
            MediaType = isFolder ? null : "Video",
            IsMovie = !isFolder,
            IsFolder = isFolder,
            IsVirtualItem = false
        };
    }

    private static LinkedChildEntity Link(Guid parentId, Guid childId, int sortOrder)
    {
        return new LinkedChildEntity
        {
            ParentId = parentId,
            ChildId = childId,
            ChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType.Manual,
            SortOrder = sortOrder
        };
    }
}
