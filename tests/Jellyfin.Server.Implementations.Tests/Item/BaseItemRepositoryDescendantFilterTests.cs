using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;
using LinkedChildType = Jellyfin.Database.Implementations.Entities.LinkedChildType;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers <see cref="InternalItemsQuery.DescendantOfId"/>, the filter a recursive query rooted at a
/// BoxSet or Playlist runs on. Those hold their contents as linked children, so the items below a
/// linked folder are only reachable by following the link and then the ancestor chain.
/// </summary>
public sealed class BaseItemRepositoryDescendantFilterTests : SqliteDbTestFixture
{
    private const string FolderType = "MediaBrowser.Controller.Entities.Folder";
    private const string BoxSetType = "MediaBrowser.Controller.Entities.Movies.BoxSet";
    private const string SeriesType = "MediaBrowser.Controller.Entities.TV.Series";
    private const string SeasonType = "MediaBrowser.Controller.Entities.TV.Season";
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";
    private const string MovieType = "MediaBrowser.Controller.Entities.Movies.Movie";

    private readonly BaseItemRepository _repository;

    private readonly Guid _library = Guid.NewGuid();
    private readonly Guid _collection = Guid.NewGuid();
    private readonly Guid _series = Guid.NewGuid();
    private readonly Guid _season = Guid.NewGuid();
    private readonly Guid _episode = Guid.NewGuid();

    // A movie the collection links directly, so the direct-child case is covered alongside the nested one.
    private readonly Guid _collectionMovie = Guid.NewGuid();

    // In the same library but outside the collection, as the control the assertions are read against.
    private readonly Guid _otherSeries = Guid.NewGuid();
    private readonly Guid _otherEpisode = Guid.NewGuid();

    public BaseItemRepositoryDescendantFilterTests()
    {
        using (var ctx = CreateDbContext())
        {
            Seed(ctx);
        }

        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    [Fact]
    public void DescendantOfId_ReachesEpisodesOfALinkedSeries()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery
        {
            DescendantOfId = _collection,
            IncludeItemTypes = [BaseItemKind.Episode]
        });

        Assert.Equal([_episode], ids);
    }

    [Fact]
    public void DescendantOfId_ReturnsEveryLevelBelowTheCollection()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { DescendantOfId = _collection }).ToHashSet();

        Assert.Equal(new[] { _series, _season, _episode, _collectionMovie }.Order(), ids.Order());
    }

    [Fact]
    public void DescendantOfId_KeepsDirectlyLinkedChildren()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery
        {
            DescendantOfId = _collection,
            IncludeItemTypes = [BaseItemKind.Movie]
        });

        Assert.Equal([_collectionMovie], ids);
    }

    [Fact]
    public void DescendantOfId_OnAnEmptyCollection_ReturnsNothing()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { DescendantOfId = Guid.NewGuid() });

        Assert.Empty(ids);
    }

    private void Seed(JellyfinDbContext context)
    {
        context.BaseItems.Add(new BaseItemEntity { Id = _library, Type = FolderType, Name = "Shows", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _collection, Type = BoxSetType, Name = "Collection", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _series, Type = SeriesType, Name = "Series", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _season, Type = SeasonType, Name = "Season 1", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _episode, Type = EpisodeType, Name = "Episode 1" });
        context.BaseItems.Add(new BaseItemEntity { Id = _collectionMovie, Type = MovieType, Name = "Movie" });
        context.BaseItems.Add(new BaseItemEntity { Id = _otherSeries, Type = SeriesType, Name = "Other series", IsFolder = true });
        context.BaseItems.Add(new BaseItemEntity { Id = _otherEpisode, Type = EpisodeType, Name = "Other episode" });

        // AncestorIds is a closure: production writes one row per ancestor, not just the parent.
        AddAncestors(context, _series, _library);
        AddAncestors(context, _season, _series, _library);
        AddAncestors(context, _episode, _season, _series, _library);
        AddAncestors(context, _collectionMovie, _library);
        AddAncestors(context, _otherSeries, _library);
        AddAncestors(context, _otherEpisode, _otherSeries, _library);

        AddLink(context, _series, 0);
        AddLink(context, _collectionMovie, 1);

        context.SaveChanges();
    }

    private void AddAncestors(JellyfinDbContext context, Guid itemId, params Guid[] ancestorIds)
    {
        foreach (var ancestorId in ancestorIds)
        {
            context.AncestorIds.Add(new AncestorId
            {
                ItemId = itemId,
                ParentItemId = ancestorId,
                Item = null!,
                ParentItem = null!
            });
        }
    }

    private void AddLink(JellyfinDbContext context, Guid childId, int sortOrder)
    {
        context.LinkedChildren.Add(new LinkedChildEntity
        {
            ParentId = _collection,
            ChildId = childId,
            ChildType = LinkedChildType.Manual,
            SortOrder = sortOrder
        });
    }
}
