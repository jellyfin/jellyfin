using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Providers;

/// <summary>
/// Marker interface for item similarity providers.
/// </summary>
public interface IItemSimilarityProvider : IMetadataProvider
{
    /// <summary>
    /// Returns whether this provider can compute similar items for the specified item.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c> if this provider handles the item.</returns>
    bool Supports(BaseItem item);

    /// <summary>
    /// Gets similar item ids for an item handled by this provider.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="limit">The maximum count of ids to consider returning.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Similar item ids.</returns>
    Task<IEnumerable<Guid>> GetSimilarItems(BaseItem item, int limit, CancellationToken cancellationToken);
}

/// <summary>
/// Interface for providing similar items to a given item.
/// </summary>
/// <typeparam name="TItemType">The type of item this provider handles.</typeparam>
public interface IItemSimilarityProvider<TItemType> : IMetadataProvider<TItemType>, IItemSimilarityProvider
    where TItemType : BaseItem
{
    /// <summary>
    /// Gets a collection of similar items for the given item.
    /// </summary>
    /// <param name="item">The item to find similar items for.</param>
    /// <param name="limit">The maximum number of items to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing an enumerable of similar item IDs.</returns>
    Task<IEnumerable<Guid>> GetSimilarItems(
        TItemType item,
        int limit,
        CancellationToken cancellationToken);

    /// <inheritdoc />
    bool IItemSimilarityProvider.Supports(BaseItem item) => item is TItemType;

    /// <inheritdoc />
    async Task<IEnumerable<Guid>> IItemSimilarityProvider.GetSimilarItems(BaseItem item, int limit, CancellationToken cancellationToken)
    {
        if (item is not TItemType typed)
        {
            return Enumerable.Empty<Guid>();
        }

        return await GetSimilarItems(typed, limit, cancellationToken).ConfigureAwait(false);
    }
}
