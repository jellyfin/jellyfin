using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SmartCollectionEntity = Jellyfin.Database.Implementations.Entities.SmartCollections;
using SmartCollectionFilters = Jellyfin.Database.Implementations.Entities.SmartCollectionFilters;

namespace MediaBrowser.Controller.SmartCollections;

/// <summary>
/// Interface for managing smart collection definitions and evaluating their filters.
/// </summary>
public interface ISmartCollectionsManager
{
    /// <summary>
    /// Creates a new smart collection for the specified user.
    /// </summary>
    /// <param name="entity">The smart collection definition to create.</param>
    /// <param name="userId">The identifier of the user creating the collection.</param>
    /// <returns>The created smart collection with its new identifier.</returns>
    Task<SmartCollectionEntity> CreateAsync(SmartCollectionEntity entity, string userId);

    /// <summary>
    /// Gets a smart collection by its identifier for the specified user.
    /// </summary>
    /// <param name="id">The identifier of the smart collection.</param>
    /// <param name="userId">The identifier of the user owning the collection.</param>
    /// <returns>The smart collection or null if not found.</returns>
    Task<SmartCollectionEntity?> GetByIdAsync(Guid id, string userId);

    /// <summary>
    /// Gets all smart collections for the specified user.
    /// </summary>
    /// <param name="userId">The identifier of the user.</param>
    /// <returns>The list of smart collections.</returns>
    Task<IEnumerable<SmartCollectionEntity>> GetAllByUserAsync(string userId);

    /// <summary>
    /// Updates an existing smart collection for the specified user.
    /// </summary>
    /// <param name="entity">The smart collection definition with updated values.</param>
    /// <param name="userId">The identifier of the user owning the collection.</param>
    /// <returns>The updated smart collection.</returns>
    Task<SmartCollectionEntity> UpdateAsync(SmartCollectionEntity entity, string userId);

    /// <summary>
    /// Deletes a smart collection by its identifier for the specified user.
    /// </summary>
    /// <param name="id">The identifier of the smart collection to delete.</param>
    /// <param name="userId">The identifier of the user owning the collection.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid id, string userId);

    /// <summary>
    /// Evaluates the specified filters and returns a list of item identifiers that match the criteria.
    /// </summary>
    /// <param name="filters">The smart collection filters to evaluate.</param>
    /// <param name="userId">The identifier of the user owning the collection.</param>
    /// <param name="limit">The maximum number of item identifiers to return.</param>
    /// <returns>A list of item identifiers that match the filter criteria.</returns>
    Task<IEnumerable<Guid>> EvaluateAsync(SmartCollectionFilters filters, string userId, int limit = 50);
}
