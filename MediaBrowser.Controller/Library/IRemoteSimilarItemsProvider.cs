using System;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Provides similar item references from remote/external sources.
/// Returns lightweight references with ProviderIds that the manager resolves to library items.
/// </summary>
public interface IRemoteSimilarItemsProvider : ISimilarItemsProvider
{
    /// <summary>
    /// Determines whether the provider can handle items of the specified type.
    /// </summary>
    /// <param name="itemType">The item type.</param>
    /// <returns><c>true</c> if the provider handles this item type; otherwise <c>false</c>.</returns>
    bool Supports(Type itemType);

    /// <summary>
    /// Gets similar item references from an external source as an async stream.
    /// </summary>
    /// <param name="item">The source item to find similar items for.</param>
    /// <param name="query">The query options (user, limit, exclusions).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of similar item references.</returns>
    IAsyncEnumerable<SimilarItemReference> GetSimilarItemsAsync(
        BaseItem item,
        SimilarItemsQuery query,
        CancellationToken cancellationToken);
}

/// <summary>
/// Provides similar item references from remote/external sources for a specific item type.
/// Returns lightweight references with ProviderIds that the manager resolves to library items.
/// </summary>
/// <typeparam name="TItemType">The type of item this provider handles.</typeparam>
public interface IRemoteSimilarItemsProvider<TItemType> : IRemoteSimilarItemsProvider
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

    bool IRemoteSimilarItemsProvider.Supports(Type itemType)
        => typeof(TItemType).IsAssignableFrom(itemType);

    IAsyncEnumerable<SimilarItemReference> IRemoteSimilarItemsProvider.GetSimilarItemsAsync(
        BaseItem item,
        SimilarItemsQuery query,
        CancellationToken cancellationToken)
        => GetSimilarItemsAsync((TItemType)item, query, cancellationToken);
}
