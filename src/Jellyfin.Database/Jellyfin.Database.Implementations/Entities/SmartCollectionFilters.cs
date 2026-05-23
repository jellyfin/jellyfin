using System;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Jellyfin.Database.Implementations.Entities
{
    /// <summary>
    /// Represents the filter criteria for a smart collection.
    /// </summary>
    public class SmartCollectionFilters
    {
        /// <summary>
        /// Gets or Sets for the last update time.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the item type.
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Gets the sort field.
        /// </summary>
        public Collection<string> Genres { get; } = new Collection<string>();

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        public int? YearFrom { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        public int? YearTo { get; set; }

        /// <summary>
        /// Gets or sets the minimum community rating (0–10).
        /// </summary>
        public float? MinCommunityRating { get; set; }

        /// <summary>
        /// Gets or sets the minimum critic rating (0–10).
        /// </summary>
        public float? MinCriticRating { get; set; }

        /// <summary>
        /// Gets the sort order.
        /// </summary>
        public Collection<string> Tags { get; } = new Collection<string>();

        /// <summary>
        /// Gets the official (PG or similar) ratings.
        /// </summary>
        public Collection<string> OfficialRatings { get; } = new Collection<string>();
    }
}
