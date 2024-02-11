using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.LiveTv.Listings.SchedulesDirectDtos
{
    /// <summary>
    /// Program dto.
    /// </summary>
    public class ProgramDto
    {
        /// <summary>
        /// Gets or sets the program id.
        /// </summary>
        [JsonPropertyName("programID")]
        public string? ProgramId { get; set; }

        /// <summary>
        /// Gets or sets the air date time.
        /// </summary>
        [JsonPropertyName("airDateTime")]
        public DateTime? AirDateTime { get; set; }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        /// <summary>
        /// Gets or sets the md5.
        /// </summary>
        [JsonPropertyName("md5")]
        public string? Md5 { get; set; }

        /// <summary>
        /// Gets or sets the list of audio properties.
        /// </summary>
        [JsonPropertyName("audioProperties")]
        public IReadOnlyList<string> AudioProperties { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the list of video properties.
        /// </summary>
        [JsonPropertyName("videoProperties")]
        public IReadOnlyList<string> VideoProperties { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets the list of ratings.
        /// </summary>
        [JsonPropertyName("ratings")]
        public IReadOnlyList<RatingDto> Ratings { get; set; } = Array.Empty<RatingDto>();

        /// <summary>
        /// Gets or sets a value indicating whether this program is new.
        /// </summary>
        [JsonPropertyName("new")]
        public bool? New { get; set; }

        /// <summary>
        /// Gets or sets the multipart object.
        /// </summary>
        [JsonPropertyName("multipart")]
        public MultipartDto? Multipart { get; set; }

        /// <summary>
        /// Gets or sets the live tape delay.
        /// </summary>
        [JsonPropertyName("liveTapeDelay")]
        public string? LiveTapeDelay { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the premiere.
        /// </summary>
        [JsonPropertyName("premiere")]
        public bool Premiere { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is a repeat.
        /// </summary>
        [JsonPropertyName("repeat")]
        public bool Repeat { get; set; }

        /// <summary>
        /// Gets or sets the premiere or finale.
        /// </summary>
        [JsonPropertyName("isPremiereOrFinale")]
        public string? IsPremiereOrFinale { get; set; }
    }
}
