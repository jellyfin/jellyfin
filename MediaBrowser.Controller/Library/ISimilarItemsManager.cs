using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;

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
}
