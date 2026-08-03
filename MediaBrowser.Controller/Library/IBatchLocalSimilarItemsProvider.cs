using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// A local similar items provider that supports batch queries across multiple source items.
/// Implementations share access filtering and entity loading across all sources for better performance.
/// </summary>
public interface IBatchLocalSimilarItemsProvider : ISimilarItemsProvider
{
    /// <summary>
    /// Gets similar items for multiple source items in a single batch.
    /// </summary>
    /// <param name="sourceItems">The source items to find similar items for.</param>
    /// <param name="query">The query options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Per-source-item results keyed by source item ID.</returns>
    Task<Dictionary<Guid, IReadOnlyList<BaseItem>>> GetBatchSimilarItemsAsync(
        IReadOnlyList<BaseItem> sourceItems,
        SimilarItemsQuery query,
        CancellationToken cancellationToken);
}
