using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Image data dto.
    /// </summary>
    public class ImageDataDto
    {
        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        [JsonPropertyName("width")]
        public string? Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        [JsonPropertyName("height")]
        public string? Height { get; set; }

        /// <summary>
        /// Gets or sets the uri.
        /// </summary>
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        [JsonPropertyName("size")]
        public string? Size { get; set; }

        /// <summary>
        /// Gets or sets the aspect.
        /// </summary>
        [JsonPropertyName("aspect")]
        public string? Aspect { get; set; }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        /// <summary>
        /// Gets or sets the primary.
        /// </summary>
        [JsonPropertyName("primary")]
        public string? Primary { get; set; }

        /// <summary>
        /// Gets or sets the tier.
        /// </summary>
        [JsonPropertyName("tier")]
        public string? Tier { get; set; }

        /// <summary>
        /// Gets or sets the caption.
        /// </summary>
        [JsonPropertyName("caption")]
        public CaptionDto? Caption { get; set; }
    }
}
