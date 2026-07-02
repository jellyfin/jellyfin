using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// NameSpace conflict with SmartCollection and it's type
using Entities = Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Server.Implementations.SmartCollections
{
    /// <summary>
    /// Interface for managing smart collection definitions.
    /// </summary>
    public interface ISmartCollectionsRepository
    {
        /// <summary>
        /// Gets all smart collections for a user.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>A list of smart collections.</returns>
        Task<IList<Entities.SmartCollections>> GetSmartCollectionsForUserAsync(Guid userId);

        /// <summary>
        /// Gets a smart collection by its identifier.
        /// </summary>
        /// <param name="id">The collection identifier.</param>
        /// <returns>The smart collection, or null if not found.</returns>
        Task<Entities.SmartCollections?> GetSmartCollectionByIdAsync(Guid id);

        /// <summary>
        /// Creates a new smart collection.
        /// </summary>
        /// <param name="collection">The smart collection to create.</param>
        /// <returns>The created smart collection with its new identifier.</returns>
        Task<Entities.SmartCollections> CreateSmartCollectionAsync(Entities.SmartCollections collection);

        /// <summary>
        /// Updates an existing smart collection.
        /// </summary>
        /// <param name="collection">The smart collection with updated values.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task UpdateSmartCollectionAsync(Entities.SmartCollections collection);

        /// <summary>
        /// Deletes a smart collection by its identifier.
        /// </summary>
        /// <param name="id">The collection identifier.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task DeleteSmartCollectionAsync(Guid id);
    }
}
