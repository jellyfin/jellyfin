// Jellyfin.Api.Models.DiscoverItemDto.cs
// This file is part of the Discover Feature for Jellyfin.
// It provides a DTO for discover search items with fields compatible with Jellyfin's API.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Api.Models
{
    /// <summary>
    /// DTO for a discover search item with Jellyfin-compatible fields.
    /// </summary>
    public class DiscoverItemDto
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [JsonPropertyName("Id")]
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [JsonPropertyName("Name")]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the production year.
        /// </summary>
        [JsonPropertyName("ProductionYear")]
        public int? ProductionYear { get; set; }

        /// <summary>
        /// Gets or sets the type of the item, e.g., "Movie" or "Series".
        /// </summary>
        [JsonPropertyName("Type")]
        public required string Type { get; set; } // "Movie" or "Series"

        /// <summary>
        /// Gets or sets the primary image tag.
        /// Use TMDb id or hash of ImageUrl if needed.
        /// </summary>
        [JsonPropertyName("PrimaryImageTag")]
        public string? PrimaryImageTag { get; set; }

        /// <summary>
        /// Gets or sets the overview.
        /// </summary>
        [JsonPropertyName("Overview")]
        public string? Overview { get; set; }

        /// <summary>
        /// Gets or sets the popularity of the item.
        /// </summary>
        [JsonPropertyName("Popularity")]
        public double? Popularity { get; set; }
    }
}
