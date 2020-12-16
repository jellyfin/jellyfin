using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using MediaBrowser.Common.Json.Converters;

namespace Jellyfin.Api.Models.PlaylistDtos
{
    /// <summary>
    /// Create new playlist dto.
    /// </summary>
    public class CreatePlaylistDto
    {
        /// <summary>
        /// Gets or sets the name of the new playlist.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets item ids to add to the playlist.
        /// </summary>
        [JsonConverter(typeof(JsonCommaDelimitedArrayConverterFactory))]
        public IReadOnlyList<Guid> Ids { get; set; } = Array.Empty<Guid>();

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets or sets the media type.
        /// </summary>
        public string? MediaType { get; set; }
    }
}
