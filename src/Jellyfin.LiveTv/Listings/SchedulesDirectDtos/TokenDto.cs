using System;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// The token dto.
    /// </summary>
    public class TokenDto
    {
        /// <summary>
        /// Gets or sets the response code.
        /// </summary>
        [JsonPropertyName("code")]
        public int Code { get; set; }

        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        [JsonPropertyName("message")]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the server id.
        /// </summary>
        [JsonPropertyName("serverID")]
        public string? ServerId { get; set; }

        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        [JsonPropertyName("token")]
        public string? Token { get; set; }

        /// <summary>
        /// Gets or sets the current datetime.
        /// </summary>
        [JsonPropertyName("datetime")]
        public DateTime? TokenTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the response message.
        /// </summary>
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
