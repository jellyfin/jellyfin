using System;
using System.Text.Json;
using Jellyfin.Database.Implementations.Enums;

namespace Jellyfin.Database.Implementations.Entities
{
    /// <summary>
    /// Represents a smart collection definition.
    /// </summary>
    public class SmartCollections
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmartCollections"/> class for entity materialization.
        /// </summary>
        protected SmartCollections()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SmartCollections"/> class.
        /// </summary>
        /// <param name="name">The collection name.</param>
        /// <param name="userId">The owning user identifier.</param>
        /// <param name="filters">The collection filters.</param>
        public SmartCollections(string name, Guid userId, SmartCollectionFilters filters)
        {
            Id = Guid.NewGuid();
            Name = name;
            UserId = userId;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            SetFilters(filters);
        }

        /// <summary>
        /// Gets or sets the collection identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the collection name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the owning user identifier.
        /// </summary>
        public Guid UserId { get; private set; }

        /// <summary>
        /// Gets the filters (JSON text).
        /// </summary>
        public string FiltersJson { get; private set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sort field.
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        public SortOrder? SortOrder { get; set; }

        /// <summary>
        /// Gets or sets the max items. Defaults to 50 to protect CPU on large libraries.
        /// </summary>
        public int Limit { get; set; } = 50;

        /// <summary>
        /// Gets the creation time in UTC.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Gets the last update time in UTC.
        /// </summary>
        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// Filters Helpers.
        /// </summary>
        /// <returns>The current collection filters.</returns>
        public SmartCollectionFilters GetFilters()
            => JsonSerializer.Deserialize<SmartCollectionFilters>(FiltersJson)
                ?? new SmartCollectionFilters();

        /// <summary>
        /// Serializes and stores the provided filters.
        /// </summary>
        /// <param name="filters">The filters to store.</param>
        public void SetFilters(SmartCollectionFilters filters)
        {
            FiltersJson = JsonSerializer.Serialize(filters);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
