using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MediaBrowser.MediaEncoding.Probing
{
    /// <summary>
    /// Class MediaFormat.
    /// </summary>
    public class MediaFormatInfo
    {
        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        /// <value>The filename.</value>
        [JsonPropertyName("filename")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the nb_streams.
        /// </summary>
        /// <value>The nb_streams.</value>
        [JsonPropertyName("nb_streams")]
        public int NbStreams { get; set; }

        /// <summary>
        /// Gets or sets the format_name.
        /// </summary>
        /// <value>The format_name.</value>
        [JsonPropertyName("format_name")]
        public string FormatName { get; set; }

        /// <summary>
        /// Gets or sets the format_long_name.
        /// </summary>
        /// <value>The format_long_name.</value>
        [JsonPropertyName("format_long_name")]
        public string FormatLongName { get; set; }

        /// <summary>
        /// Gets or sets the start_time.
        /// </summary>
        /// <value>The start_time.</value>
        [JsonPropertyName("start_time")]
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        /// <value>The duration.</value>
        [JsonPropertyName("duration")]
        public string Duration { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [JsonPropertyName("size")]
        public string Size { get; set; }

        /// <summary>
        /// Gets or sets the bit_rate.
        /// </summary>
        /// <value>The bit_rate.</value>
        [JsonPropertyName("bit_rate")]
        public string BitRate { get; set; }

        /// <summary>
        /// Gets or sets the probe_score.
        /// </summary>
        /// <value>The probe_score.</value>
        [JsonPropertyName("probe_score")]
        public int ProbeScore { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        [JsonPropertyName("tags")]
        public IReadOnlyDictionary<string, string> Tags { get; set; }
    }
}
