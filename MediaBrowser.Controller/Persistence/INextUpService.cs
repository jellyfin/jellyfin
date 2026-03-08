using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Persistence;

/// <summary>
/// Provides next-up episode query operations.
/// </summary>
public interface INextUpService
{
    /// <summary>
    /// Gets the list of series presentation keys for next up.
    /// </summary>
    /// <param name="filter">The query.</param>
    /// <param name="dateCutoff">The minimum date for a series to have been most recently watched.</param>
    /// <returns>The list of keys.</returns>
    IReadOnlyList<string> GetNextUpSeriesKeys(InternalItemsQuery filter, DateTime dateCutoff);

    /// <summary>
    /// Gets next up episodes for multiple series in a single batched query.
    /// </summary>
    /// <param name="filter">The query filter.</param>
    /// <param name="seriesKeys">The series presentation unique keys to query.</param>
    /// <param name="includeSpecials">Whether to include specials.</param>
    /// <param name="includeWatchedForRewatching">Whether to include watched episodes for rewatching mode.</param>
    /// <returns>A dictionary mapping series key to batch result.</returns>
    IReadOnlyDictionary<string, NextUpEpisodeBatchResult> GetNextUpEpisodesBatch(
        InternalItemsQuery filter,
        IReadOnlyList<string> seriesKeys,
        bool includeSpecials,
        bool includeWatchedForRewatching);
}
