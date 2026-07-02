using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.SmartCollections;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Entities = Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Server.Implementations.SmartCollections
{
    /// <summary>
    /// Implementation of the smart collections manager.
    /// </summary>
    public class SmartCollectionsManager : ISmartCollectionsManager
    {
        private const int CacheExpirationMinutes = 10;
        private readonly ISmartCollectionsRepository _repository;
        private readonly IUserManager _userManager;
        private readonly ILogger<SmartCollectionsManager> _logger;
        private readonly IItemRepository _itemRepository;
        private readonly IMemoryCache _cache;

        // Cache expiration time. Avoid multi evaluations within a short time.

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartCollectionsManager"/> class.
        /// </summary>
        /// <param name="repository">The smart collections repository.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="itemRepository">The item repository.</param>
        /// <param name="cache">The memory cache.</param>
        public SmartCollectionsManager(
            ISmartCollectionsRepository repository,
            IUserManager userManager,
            ILogger<SmartCollectionsManager> logger,
            IItemRepository itemRepository,
            IMemoryCache cache)
        {
            _repository = repository;
            _userManager = userManager;
            _logger = logger;
            _itemRepository = itemRepository;
            _cache = cache;
        }

        /// <summary>
        /// Creates a new smart collection for the specified user.
        /// </summary>
        /// <param name="entity">The smart collection definition.</param>
        /// <param name="userId">The identifier of the user owning the collection.</param>
        /// <returns>The created smart collection.</returns>
        /// <exception cref="ArgumentException">Thrown when the userId is invalid.</exception>
        public async Task<Entities.SmartCollections> CreateAsync(Entities.SmartCollections entity, string userId)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new ArgumentException("Invalid userId GUID.", nameof(userId));
            }

            var filters = entity.GetFilters();
            filters.UpdatedAt = DateTime.UtcNow;
            var newEntity = new Entities.SmartCollections(entity.Name, userGuid, filters)
            {
                Limit = entity.Limit,
                SortBy = entity.SortBy,
                SortOrder = entity.SortOrder
            };

            var created = await _repository.CreateSmartCollectionAsync(newEntity).ConfigureAwait(false);
            return created;
        }

        /// <summary>
        /// Gets a smart collection by its identifier for the specified user.
        /// </summary>
        /// <param name="id">The identifier of the smart collection.</param>
        /// <param name="userId">The identifier of the user owning the collection.</param>
        /// <returns>The smart collection or null if not found.</returns>
        public async Task<Entities.SmartCollections?> GetByIdAsync(Guid id, string userId)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new ArgumentException("Invalid userId GUID.", nameof(userId));
            }

            return await _repository.GetSmartCollectionByIdAsync(id).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all smart collections for the specified user.
        /// </summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <returns>The list of smart collections.</returns>
        public async Task<IEnumerable<Entities.SmartCollections>> GetAllByUserAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new ArgumentException("Invalid userId GUID.", nameof(userId));
            }

            return await _repository.GetSmartCollectionsForUserAsync(userGuid).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an existing smart collection for the specified user.
        /// </summary>
        /// <param name="entity">The smart collection definition with updated values.</param>
        /// <param name="userId">The identifier of the user owning the collection.</param>
        /// <returns>The updated smart collection.</returns>
        public async Task<Entities.SmartCollections> UpdateAsync(Entities.SmartCollections entity, string userId)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new ArgumentException("Invalid userId GUID.", nameof(userId));
            }

            var existing = await _repository.GetSmartCollectionByIdAsync(entity.Id).ConfigureAwait(false);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Smart collection with ID {entity.Id} not found.");
            }

            if (!existing.UserId.Equals(userGuid))
            {
                throw new UnauthorizedAccessException("User does not own this smart collection.");
            }

            existing.Name = entity.Name;
            var filters = entity.GetFilters();
            filters.UpdatedAt = DateTime.UtcNow;
            existing.SetFilters(filters);
            existing.Limit = entity.Limit;
            existing.SortBy = entity.SortBy;
            existing.SortOrder = entity.SortOrder;

            await _repository.UpdateSmartCollectionAsync(existing).ConfigureAwait(false);
            return existing;
        }

        /// <summary>
        /// Deletes a smart collection by its identifier for the specified user.
        /// </summary>
        /// <param name="id">The identifier of the smart collection to delete.</param>
        /// <param name="userId">The identifier of the user owning the collection.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task DeleteAsync(Guid id, string userId)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new ArgumentException("Invalid userId GUID.", nameof(userId));
            }

            var existing = await _repository.GetSmartCollectionByIdAsync(id).ConfigureAwait(false);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Smart collection with ID {id} not found.");
            }

            if (!existing.UserId.Equals(userGuid))
            {
                throw new UnauthorizedAccessException("User does not own this smart collection.");
            }

            await _repository.DeleteSmartCollectionAsync(id).ConfigureAwait(false);
        }

        /// <summary>
        /// Evaluates the specified filters and returns a list of item identifiers that match the criteria.
        /// </summary> <param name="filters">The smart collection filters to evaluate.</param>
        /// <param name="userId">The identifier of the user owning the collection.</param>
        /// <param name="limit">The maximum number of item identifiers to return.</param>
        /// <returns>A list of item identifiers that match the filter criteria.</returns>
        public async Task<IEnumerable<Guid>> EvaluateAsync(Entities.SmartCollectionFilters filters, string userId, int limit = 50)
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new ArgumentException("Invalid userId GUID.", nameof(userId));
            }

            var cacheKey = $"{userGuid}:{JsonSerializer.Serialize(filters)}:{limit}";

            if (_cache.TryGetValue(cacheKey, out var cachedResultObj) && cachedResultObj is IEnumerable<Guid> cachedResult)
            {
                _logger.LogInformation("Smart collection evaluation cache hit for user {UserId}.", userGuid);
                return cachedResult;
            }

            var user = _userManager.GetUserById(userGuid);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userGuid} not found.");
            }

            var query = MapFiltersToQuery(filters, user, limit);

            var ids = await Task.Run(() => _itemRepository.GetItemIdsList(query)).ConfigureAwait(false);

            _cache.Set(
                cacheKey,
                ids,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
                });

            return ids;
        }

        private static InternalItemsQuery MapFiltersToQuery(Entities.SmartCollectionFilters filters, Entities.User user, int limit)
        {
            var query = new InternalItemsQuery(user)
            {
                Limit = limit
            };

            if (filters.Genres?.Count > 0)
            {
                query.Genres = filters.Genres.ToArray();
            }

            if (filters.Tags?.Count > 0)
            {
                query.Tags = filters.Tags.ToArray();
            }

            if (filters.YearFrom.HasValue || filters.YearTo.HasValue)
            {
                var from = filters.YearFrom ?? 1900;
                var to = filters.YearTo ?? DateTime.UtcNow.Year;

                query.Years = Enumerable.Range(from, to - from + 1).ToArray();
            }

            if (filters.MinCommunityRating.HasValue)
            {
                query.MinCommunityRating = filters.MinCommunityRating.Value;
            }

            if (filters.MinCriticRating.HasValue)
            {
                query.MinCriticRating = filters.MinCriticRating.Value;
            }

            if (filters.OfficialRatings?.Count > 0)
            {
                query.OfficialRatings = filters.OfficialRatings.ToArray();
            }

            return query;
        }
    }
}
