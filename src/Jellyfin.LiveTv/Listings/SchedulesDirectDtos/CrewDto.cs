using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Crew dto.
    /// </summary>
    public class CrewDto
    {
        /// <summary>
        /// Gets or sets the billing order.
        /// </summary>
        [JsonPropertyName("billingOrder")]
        public string? BillingOrder { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        /// <summary>
        /// Gets or sets the name id.
        /// </summary>
        [JsonPropertyName("nameId")]
        public string? NameId { get; set; }

        /// <summary>
        /// Gets or sets the person id.
        /// </summary>
        [JsonPropertyName("personId")]
        public string? PersonId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
