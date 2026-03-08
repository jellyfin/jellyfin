using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Provides similar item references from remote/external sources for a specific item type.
/// Returns lightweight references with ProviderIds that the manager resolves to library items.
/// </summary>
/// <typeparam name="TItemType">The type of item this provider handles.</typeparam>
public interface IRemoteSimilarItemsProvider<TItemType> : ISimilarItemsProvider
    where TItemType : BaseItem
{
    /// <summary>
    /// Gets similar item references from an external source as an async stream.
    /// </summary>
    /// <param name="item">The source item to find similar items for.</param>
    /// <param name="query">The query options (user, limit, exclusions).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of similar item references.</returns>
    IAsyncEnumerable<SimilarItemReference> GetSimilarItemsAsync(
        TItemType item,
        SimilarItemsQuery query,
        CancellationToken cancellationToken);
}
