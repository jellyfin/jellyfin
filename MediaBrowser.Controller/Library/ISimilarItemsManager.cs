using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Interface for managing similar items providers and operations.
/// </summary>
public interface ISimilarItemsManager
{
    /// <summary>
    /// Registers similar items providers discovered through dependency injection.
    /// </summary>
    /// <param name="providers">The similar items providers to register.</param>
    void AddParts(IEnumerable<ISimilarItemsProvider> providers);

    /// <summary>
    /// Gets the similar items providers for a specific item type.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <returns>The list of similar items providers for that type.</returns>
    IReadOnlyList<ISimilarItemsProvider> GetSimilarItemsProviders<T>()
        where T : BaseItem;

    /// <summary>
    /// Gets similar items for the specified item.
    /// </summary>
    /// <param name="item">The source item to find similar items for.</param>
    /// <param name="excludeArtistIds">Artist IDs to exclude from results.</param>
    /// <param name="user">The user context.</param>
    /// <param name="dtoOptions">The DTO options.</param>
    /// <param name="limit">Maximum number of results.</param>
    /// <param name="libraryOptions">The library options for provider configuration.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of similar items.</returns>
    Task<IReadOnlyList<BaseItem>> GetSimilarItemsAsync(
        BaseItem item,
        IReadOnlyList<Guid> excludeArtistIds,
        User? user,
        DtoOptions dtoOptions,
        int? limit,
        LibraryOptions? libraryOptions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Builds movie recommendations for a user: a mix of similar-items and person-based categories,
    /// scheduled round-robin and capped to <paramref name="categoryLimit"/>.
    /// </summary>
    /// <param name="user">The user the recommendations are for. May be <see langword="null"/> for anonymous access.</param>
    /// <param name="parentId">The library/folder to localize the search to. Pass <see cref="Guid.Empty"/> to use the root.</param>
    /// <param name="categoryLimit">Maximum number of recommendation categories to return.</param>
    /// <param name="itemLimit">Maximum number of items per category.</param>
    /// <param name="dtoOptions">DTO options used when querying the library.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of recommendation categories, ordered by <see cref="RecommendationType"/>.</returns>
    Task<IReadOnlyList<SimilarItemsRecommendation>> GetMovieRecommendationsAsync(
        User? user,
        Guid parentId,
        int categoryLimit,
        int itemLimit,
        DtoOptions dtoOptions,
        CancellationToken cancellationToken);
}
