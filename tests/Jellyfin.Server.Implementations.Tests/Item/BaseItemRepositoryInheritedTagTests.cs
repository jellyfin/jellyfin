using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the inherited-tag filters a user's blocked and allowed tags turn into. A tag reaches an item
/// four ways - on the item, on its series, on an ancestor and on its library - and each of them has to
/// keep answering the same after the ancestor check moved off a per-row correlated subquery.
/// </summary>
public sealed class BaseItemRepositoryInheritedTagTests : SqliteDbTestFixture
{
    private const string FolderType = "MediaBrowser.Controller.Entities.Folder";
    private const string SeriesType = "MediaBrowser.Controller.Entities.TV.Series";
    private const string SeasonType = "MediaBrowser.Controller.Entities.TV.Season";
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";

    private const string Tag = "Adult";

    private readonly BaseItemRepository _repository;

    private readonly Guid _library = Guid.NewGuid();
    private readonly Guid _otherLibrary = Guid.NewGuid();

    // Tagged on the item itself.
    private readonly Guid _taggedSeries = Guid.NewGuid();

    // Inherits the tag from the series it belongs to, without an ancestor row for it.
    private readonly Guid _episodeOfTaggedSeries = Guid.NewGuid();

    // Inherits the tag from a season in the middle of its ancestor chain.
    private readonly Guid _taggedSeason = Guid.NewGuid();
    private readonly Guid _episodeUnderTaggedSeason = Guid.NewGuid();

    // Inherits the tag from the library above it.
    private readonly Guid _taggedLibrarySeries = Guid.NewGuid();

    // Carries the tag nowhere, the control the assertions are read against.
    private readonly Guid _untaggedSeries = Guid.NewGuid();
    private readonly Guid _untaggedEpisode = Guid.NewGuid();

    public BaseItemRepositoryInheritedTagTests()
    {
        using (var ctx = CreateDbContext())
        {
            Seed(ctx);
        }

        _repository = CreateBaseItemRepository(new ItemTypeLookup());
    }

    [Fact]
    public void ExcludeInheritedTags_DropsEveryItemTheTagReaches()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { ExcludeInheritedTags = [Tag] }).ToHashSet();

        // The blocked library carries the tag itself, so it goes with everything under it.
        Assert.Equal(
            new[] { _library, _untaggedSeries, _untaggedEpisode }.Order(),
            ids.Order());
    }

    [Fact]
    public void IncludeInheritedTags_KeepsExactlyTheItemsTheTagReaches()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { IncludeInheritedTags = [Tag] }).ToHashSet();

        Assert.Equal(
            new[] { _otherLibrary, _taggedSeries, _episodeOfTaggedSeries, _taggedSeason, _episodeUnderTaggedSeason, _taggedLibrarySeries }.Order(),
            ids.Order());
    }

    [Fact]
    public void ExcludeInheritedTags_DropsAnItemReachedOnlyThroughAnAncestor()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Episode],
            ExcludeInheritedTags = [Tag]
        });

        Assert.Equal([_untaggedEpisode], ids);
    }

    [Fact]
    public void ExcludeInheritedTags_WithAnUnusedTag_KeepsEverything()
    {
        var ids = _repository.GetItemIdsList(new InternalItemsQuery { ExcludeInheritedTags = ["Unused"] });

        Assert.Equal(9, ids.Count);
    }

    private void Seed(JellyfinDbContext context)
    {
        AddItem(context, _library, FolderType, "Shows", true);
        AddItem(context, _otherLibrary, FolderType, "Blocked library", true);
        AddItem(context, _taggedSeries, SeriesType, "Tagged series", true);
        AddItem(context, _episodeOfTaggedSeries, EpisodeType, "Episode of tagged series", false, _taggedSeries);
        AddItem(context, _taggedSeason, SeasonType, "Tagged season", true);
        AddItem(context, _episodeUnderTaggedSeason, EpisodeType, "Episode under tagged season", false);
        AddItem(context, _taggedLibrarySeries, SeriesType, "Series in blocked library", true);
        AddItem(context, _untaggedSeries, SeriesType, "Untagged series", true);
        AddItem(context, _untaggedEpisode, EpisodeType, "Untagged episode", false, _untaggedSeries);

        // AncestorIds is a closure: production writes one row per ancestor, not just the parent. The
        // episode of the tagged series deliberately has none, so the series branch is what has to catch it.
        AddAncestors(context, _taggedSeries, _library);
        AddAncestors(context, _taggedSeason, _library);
        AddAncestors(context, _episodeUnderTaggedSeason, _taggedSeason, _library);
        AddAncestors(context, _taggedLibrarySeries, _otherLibrary);
        AddAncestors(context, _untaggedSeries, _library);
        AddAncestors(context, _untaggedEpisode, _untaggedSeries, _library);

        Tagged(context, _taggedSeries, _taggedSeason, _otherLibrary);

        context.SaveChanges();
    }

    private void AddItem(JellyfinDbContext context, Guid id, string type, string name, bool isFolder, Guid? seriesId = null)
    {
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = type,
            Name = name,
            IsFolder = isFolder,
            SeriesId = seriesId
        });
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

    private void Tagged(JellyfinDbContext context, params Guid[] itemIds)
    {
        var itemValue = new ItemValue
        {
            ItemValueId = Guid.NewGuid(),
            Type = ItemValueType.Tags,
            Value = Tag,
            CleanValue = Tag.GetCleanValue()
        };

        context.ItemValues.Add(itemValue);
        foreach (var itemId in itemIds)
        {
            context.ItemValuesMap.Add(new ItemValueMap
            {
                ItemId = itemId,
                ItemValueId = itemValue.ItemValueId,
                Item = null!,
                ItemValue = null!
            });
        }
    }
}
