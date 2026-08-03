using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Provides similar items from the local library.
/// Returns fully resolved BaseItems directly - no additional resolution needed.
/// </summary>
public interface ILocalSimilarItemsProvider : ISimilarItemsProvider
{
    /// <summary>
    /// Determines whether the provider can handle items of the specified type.
    /// </summary>
    /// <param name="itemType">The item type.</param>
    /// <returns><c>true</c> if the provider handles this item type; otherwise <c>false</c>.</returns>
    bool Supports(Type itemType);

    /// <summary>
    /// Gets similar items from the local library.
    /// </summary>
    /// <param name="item">The source item to find similar items for.</param>
    /// <param name="query">The query options (user, limit, exclusions, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of similar items from the library.</returns>
    Task<IReadOnlyList<BaseItem>> GetSimilarItemsAsync(
        BaseItem item,
        SimilarItemsQuery query,
        CancellationToken cancellationToken);
}

/// <summary>
/// Provides similar items from the local library for a specific item type.
/// Returns fully resolved BaseItems directly - no additional resolution needed.
/// </summary>
/// <typeparam name="TItemType">The type of item this provider handles.</typeparam>
public interface ILocalSimilarItemsProvider<TItemType> : ILocalSimilarItemsProvider
    where TItemType : BaseItem
{
    /// <summary>
    /// Gets similar items from the local library.
    /// </summary>
    /// <param name="item">The source item to find similar items for.</param>
    /// <param name="query">The query options (user, limit, exclusions, etc.).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The list of similar items from the library.</returns>
    Task<IReadOnlyList<BaseItem>> GetSimilarItemsAsync(
        TItemType item,
        SimilarItemsQuery query,
        CancellationToken cancellationToken);

    bool ILocalSimilarItemsProvider.Supports(Type itemType)
        => typeof(TItemType).IsAssignableFrom(itemType);

    Task<IReadOnlyList<BaseItem>> ILocalSimilarItemsProvider.GetSimilarItemsAsync(
        BaseItem item,
        SimilarItemsQuery query,
        CancellationToken cancellationToken)
        => GetSimilarItemsAsync((TItemType)item, query, cancellationToken);
}
