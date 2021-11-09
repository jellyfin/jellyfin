using System.Text.Json.Serialization;

namespace Emby.Server.Implementations.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Description 100 dto.
    /// </summary>
    public class Description100Dto
    {
        /// <summary>
        /// Gets or sets the description language.
        /// </summary>
        [JsonPropertyName("descriptionLanguage")]
        public string? DescriptionLanguage { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
