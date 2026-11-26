using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Microsoft.EntityFrameworkCore;
using Entities = Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Server.Implementations.SmartCollections
{
    /// <summary>
    /// SQLite implementation of the smart collections repository.
    /// </summary>
    public class SmartCollectionsRepository : ISmartCollectionsRepository
    {
        private readonly IDbContextFactory<JellyfinDbContext> _dbContextFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartCollectionsRepository"/> class.
        /// </summary>
        /// <param name="dbContextFactory">The database context factory.</param>
        public SmartCollectionsRepository(IDbContextFactory<JellyfinDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Gets all smart collections for a user.
        /// </summary>
        /// <param name="userId">The user ID.</param>
        /// <returns>The list of smart collections.</returns>
        public async Task<IList<Entities.SmartCollections>> GetSmartCollectionsForUserAsync(Guid userId)
        {
            var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            try
            {
                return await dbContext.Set<Entities.SmartCollections>()
                    .AsNoTracking()
                    .Where(collection => collection.UserId.Equals(userId))
                    .OrderBy(collection => collection.Name)
                    .ToListAsync()
                    .ConfigureAwait(false);
            }
            finally
            {
                if (dbContext != null)
                {
                    await dbContext.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Gets a smart collection by its identifier.
        /// </summary>
        /// <param name="id">The smart collection ID.</param>
        /// <returns>The smart collection or null if not found.</returns>
        public async Task<Entities.SmartCollections?> GetSmartCollectionByIdAsync(Guid id)
        {
            var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            try
            {
                return await dbContext.Set<Entities.SmartCollections>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(collection => collection.Id.Equals(id))
                    .ConfigureAwait(false);
            }
            finally
            {
                if (dbContext != null)
                {
                    await dbContext.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Creates a new smart collection.
        /// </summary>
        /// <param name="collection">The smart collection to create.</param>
        /// <returns>The created smart collection.</returns>
        public async Task<Entities.SmartCollections> CreateSmartCollectionAsync(Entities.SmartCollections collection)
        {
            var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            try
            {
                dbContext.Set<Entities.SmartCollections>().Add(collection);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
                return collection;
            }
            finally
            {
                if (dbContext != null)
                {
                    await dbContext.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Updates an existing smart collection.
         /// </summary>
         /// <param name="collection">The smart collection with updated values.</param>
         /// <returns>A task representing the asynchronous operation.</returns>
        public async Task UpdateSmartCollectionAsync(Entities.SmartCollections collection)
        {
            var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            try
            {
                dbContext.Set<Entities.SmartCollections>().Update(collection);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            finally
            {
                if (dbContext != null)
                {
                    await dbContext.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Deletes a smart collection by its identifier.
         /// </summary>
         /// <param name="id">The smart collection ID.</param>
         /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteSmartCollectionAsync(Guid id)
        {
            var dbContext = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
            try
            {
                var collection = await dbContext.Set<Entities.SmartCollections>()
                    .FirstOrDefaultAsync(entity => entity.Id.Equals(id))
                    .ConfigureAwait(false);

                if (collection is null)
                {
                    return;
                }

                dbContext.Remove(collection);
                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            finally
            {
                if (dbContext != null)
                {
                    await dbContext.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
