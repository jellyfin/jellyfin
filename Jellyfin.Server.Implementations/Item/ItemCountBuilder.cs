using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;

namespace Jellyfin.Server.Implementations.Item;

/// <summary>
/// Turns per-type counts into an <see cref="ItemCounts"/>.
/// </summary>
internal static class ItemCountBuilder
{
    /// <summary>
    /// Builds the counts of one by-name item.
    /// </summary>
    /// <param name="itemTypeLookup">The item type lookup.</param>
    /// <param name="counts">The counted items, by type name. A type may repeat.</param>
    /// <returns>The counts.</returns>
    public static ItemCounts Build(IItemTypeLookup itemTypeLookup, IEnumerable<(string Type, int Count)> counts)
    {
        ArgumentNullException.ThrowIfNull(itemTypeLookup);
        ArgumentNullException.ThrowIfNull(counts);

        var lookup = itemTypeLookup.BaseItemKindNames;
        var result = new ItemCounts();

        foreach (var (type, count) in counts)
        {
            // Accumulated rather than assigned: a caller may group by something finer than the
            // type and hand the same type over more than once.
            if (string.Equals(type, lookup[BaseItemKind.MusicAlbum], StringComparison.Ordinal))
            {
                result.AlbumCount += count;
            }
            else if (string.Equals(type, lookup[BaseItemKind.MusicArtist], StringComparison.Ordinal))
            {
                result.ArtistCount += count;
            }
            else if (string.Equals(type, lookup[BaseItemKind.Episode], StringComparison.Ordinal))
            {
                result.EpisodeCount += count;
            }
            else if (string.Equals(type, lookup[BaseItemKind.Movie], StringComparison.Ordinal))
            {
                result.MovieCount += count;
            }
            else if (string.Equals(type, lookup[BaseItemKind.MusicVideo], StringComparison.Ordinal))
            {
                result.MusicVideoCount += count;
            }
            else if (string.Equals(type, lookup[BaseItemKind.LiveTvProgram], StringComparison.Ordinal))
            {
                result.ProgramCount += count;
            }
            else if (string.Equals(type, lookup[BaseItemKind.Series], StringComparison.Ordinal))
            {
                result.SeriesCount += count;
            }
            else if (string.Equals(type, lookup[BaseItemKind.Audio], StringComparison.Ordinal))
            {
                result.SongCount += count;
            }
            else if (string.Equals(type, lookup[BaseItemKind.Trailer], StringComparison.Ordinal))
            {
                result.TrailerCount += count;
            }
            else if (string.Equals(type, lookup[BaseItemKind.BoxSet], StringComparison.Ordinal))
            {
                result.BoxSetCount += count;
            }
            else if (string.Equals(type, lookup[BaseItemKind.Book], StringComparison.Ordinal))
            {
                result.BookCount += count;
            }
        }

        result.ItemCount = result.TotalItemCount();

        return result;
    }

    /// <summary>
    /// Replaces the episode count, which both by-name paths decide separately from the other
    /// types because a genre or studio is usually written on the series rather than its episodes.
    /// </summary>
    /// <param name="counts">The counts to update.</param>
    /// <param name="episodeCount">The episode count.</param>
    public static void SetEpisodeCount(ItemCounts counts, int episodeCount)
    {
        ArgumentNullException.ThrowIfNull(counts);

        counts.EpisodeCount = episodeCount;
        counts.ItemCount = counts.TotalItemCount();
    }
}
