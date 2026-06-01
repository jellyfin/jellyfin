using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library.Recommendations;

namespace Emby.Server.Implementations.Library.Recommendations;

/// <summary>
/// Pure transformation from a user's watch/favorite history into a <see cref="TasteProfile"/>.
/// </summary>
public static class TasteProfileBuilder
{
    /// <summary>The signal weight applied when an item has been played.</summary>
    internal const float WatchedSignal = 1.0f;

    /// <summary>The signal weight applied when an item is a favorite.</summary>
    internal const float FavoriteSignal = 2.0f;

    /// <summary>The signal weight applied when an item has been liked.</summary>
    internal const float LikedSignal = 1.5f;

    /// <summary>The multiplier applied to each genre match.</summary>
    internal const float GenreFieldWeight = 3.0f;

    /// <summary>The multiplier applied to each tag match.</summary>
    internal const float TagFieldWeight = 1.5f;

    /// <summary>The multiplier applied to each person match.</summary>
    internal const float PersonFieldWeight = 1.0f;

    /// <summary>The multiplier applied to each studio match.</summary>
    internal const float StudioFieldWeight = 0.5f;

    /// <summary>Maximum number of people considered per item (top-N by ascending SortOrder).</summary>
    internal const int MaxPeoplePerItem = 5;

    /// <summary>
    /// Builds a <see cref="TasteProfile"/> by aggregating weighted metadata signals
    /// from the supplied history items.
    /// </summary>
    /// <param name="kind">The item kind to build the profile for; items of other kinds are ignored.</param>
    /// <param name="historyItems">The full set of history items (all kinds).</param>
    /// <param name="isPlayed">Returns <see langword="true"/> when the item has been played by the user.</param>
    /// <param name="isFavorite">Returns <see langword="true"/> when the item is a favorite of the user.</param>
    /// <param name="isLiked">Returns <see langword="true"/> when the item has been liked by the user.</param>
    /// <param name="peopleByItem">A pre-fetched map of item ID → associated people.</param>
    /// <returns>An immutable <see cref="TasteProfile"/> reflecting the aggregated signals.</returns>
    public static TasteProfile Build(
        BaseItemKind kind,
        IReadOnlyList<BaseItem> historyItems,
        Func<BaseItem, bool> isPlayed,
        Func<BaseItem, bool> isFavorite,
        Func<BaseItem, bool> isLiked,
        IReadOnlyDictionary<Guid, IReadOnlyList<PersonInfo>> peopleByItem)
    {
        var genres = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        var tags = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        var studios = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        var people = new Dictionary<Guid, float>();

        foreach (var item in historyItems)
        {
            if (item.GetBaseItemKind() != kind)
            {
                continue;
            }

            var signal =
                (isPlayed(item) ? WatchedSignal : 0f) +
                (isFavorite(item) ? FavoriteSignal : 0f) +
                (isLiked(item) ? LikedSignal : 0f);

            if (signal <= 0)
            {
                continue;
            }

            foreach (var g in item.Genres ?? Array.Empty<string>())
            {
                if (string.IsNullOrEmpty(g))
                {
                    continue;
                }

                genres.TryGetValue(g, out var w);
                genres[g] = w + (signal * GenreFieldWeight);
            }

            foreach (var t in item.Tags ?? Array.Empty<string>())
            {
                if (string.IsNullOrEmpty(t))
                {
                    continue;
                }

                tags.TryGetValue(t, out var w);
                tags[t] = w + (signal * TagFieldWeight);
            }

            foreach (var s in item.Studios ?? Array.Empty<string>())
            {
                if (string.IsNullOrEmpty(s))
                {
                    continue;
                }

                studios.TryGetValue(s, out var w);
                studios[s] = w + (signal * StudioFieldWeight);
            }

            if (peopleByItem.TryGetValue(item.Id, out var itemPeople))
            {
                foreach (var p in itemPeople
                    .OrderBy(p => p.SortOrder ?? int.MaxValue)
                    .Take(MaxPeoplePerItem))
                {
                    people.TryGetValue(p.Id, out var w);
                    people[p.Id] = w + (signal * PersonFieldWeight);
                }
            }
        }

        var total =
            genres.Values.Sum() +
            tags.Values.Sum() +
            studios.Values.Sum() +
            people.Values.Sum();

        return new TasteProfile(
            kind,
            DateTime.UtcNow,
            genres,
            tags,
            people,
            studios,
            total);
    }
}
