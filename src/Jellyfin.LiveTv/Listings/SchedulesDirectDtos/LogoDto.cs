using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Logo dto.
    /// </summary>
    public class LogoDto
    {
        /// <summary>
        /// Gets or sets the url.
        /// </summary>
        [JsonPropertyName("URL")]
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        [JsonPropertyName("height")]
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        [JsonPropertyName("width")]
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the md5.
        /// </summary>
        [JsonPropertyName("md5")]
        public string? Md5 { get; set; }
    }
}
