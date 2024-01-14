using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Multipart dto.
    /// </summary>
    public class MultipartDto
    {
        /// <summary>
        /// Gets or sets the part number.
        /// </summary>
        [JsonPropertyName("partNumber")]
        public int PartNumber { get; set; }

        /// <summary>
        /// Gets or sets the total parts.
        /// </summary>
        [JsonPropertyName("totalParts")]
        public int TotalParts { get; set; }
    }
}
