using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using Xunit;
using LinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class BaseItemRepositoryHideFromLibraryTests : SqliteDbTestFixture
{
    private const string MovieType = "MediaBrowser.Controller.Entities.Movies.Movie";
    private const string BoxSetType = "MediaBrowser.Controller.Entities.Movies.BoxSet";

    private readonly BaseItemRepository _repository;
    private readonly Guid _visibleMovie = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _hiddenMovie = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private readonly Guid _groupedMovie = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private readonly Guid _hidingCollection = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private readonly Guid _normalCollection = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    public BaseItemRepositoryHideFromLibraryTests()
    {
        _repository = CreateBaseItemRepository(new ItemTypeLookup());
        Seed();
    }

    [Fact]
    public void GetItemList_HidesMembersOfHidingCollection()
    {
        var ids = _repository.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie]
        }).Select(i => i.Id).ToHashSet();

        Assert.Contains(_visibleMovie, ids);
        Assert.Contains(_groupedMovie, ids);
        Assert.DoesNotContain(_hiddenMovie, ids);
    }

    [Fact]
    public void GetItemList_ShowsHiddenMembersWhenViewingTheCollection()
    {
        var ids = _repository.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            DescendantOfId = _hidingCollection
        }).Select(i => i.Id).ToHashSet();

        Assert.Single(ids);
        Assert.Contains(_hiddenMovie, ids);
    }

    [Fact]
    public void GetItemList_CanDisableHideFilter()
    {
        var ids = _repository.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            ExcludeItemsHiddenByCollections = false
        }).Select(i => i.Id).ToHashSet();

        Assert.Contains(_hiddenMovie, ids);
        Assert.Contains(_visibleMovie, ids);
        Assert.Contains(_groupedMovie, ids);
    }

    private void Seed()
    {
        using var context = CreateDbContext();

        context.BaseItems.Add(CreateMovie(_visibleMovie, "Visible movie"));
        context.BaseItems.Add(CreateMovie(_hiddenMovie, "Hidden movie"));
        context.BaseItems.Add(CreateMovie(_groupedMovie, "Grouped movie"));
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _hidingCollection,
            Type = BoxSetType,
            Name = "Private",
            IsFolder = true,
            Data = "{" + BoxSet.HideItemsFromLibraryDataMarker + "}"
        });
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = _normalCollection,
            Type = BoxSetType,
            Name = "MCU",
            IsFolder = true,
            Data = "{\"HideItemsFromLibrary\":false}"
        });

        context.LinkedChildren.Add(new LinkedChildEntity
        {
            ParentId = _hidingCollection,
            ChildId = _hiddenMovie,
            ChildType = LinkedChildType.Manual,
            SortOrder = 0
        });
        context.LinkedChildren.Add(new LinkedChildEntity
        {
            ParentId = _normalCollection,
            ChildId = _groupedMovie,
            ChildType = LinkedChildType.Manual,
            SortOrder = 0
        });

        context.SaveChanges();
    }

    private static BaseItemEntity CreateMovie(Guid id, string name)
    {
        return new BaseItemEntity
        {
            Id = id,
            Type = MovieType,
            Name = name,
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        };
    }
}
