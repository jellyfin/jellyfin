using System.Text.Json.Serialization;

namespace MediaBrowser.MediaEncoding.Probing
{
    /// <summary>
    /// Class MediaStreamInfoSideData.
    /// </summary>
    public class MediaStreamInfoSideData
    {
        /// <summary>
        /// Gets or sets the SideDataType.
        /// </summary>
        /// <value>The SideDataType.</value>
        [JsonPropertyName("side_data_type")]
        public string? SideDataType { get; set; }

        /// <summary>
        /// Gets or sets the DvVersionMajor.
        /// </summary>
        /// <value>The DvVersionMajor.</value>
        [JsonPropertyName("dv_version_major")]
        public int? DvVersionMajor { get; set; }

        /// <summary>
        /// Gets or sets the DvVersionMinor.
        /// </summary>
        /// <value>The DvVersionMinor.</value>
        [JsonPropertyName("dv_version_minor")]
        public int? DvVersionMinor { get; set; }

        /// <summary>
        /// Gets or sets the DvProfile.
        /// </summary>
        /// <value>The DvProfile.</value>
        [JsonPropertyName("dv_profile")]
        public int? DvProfile { get; set; }

        /// <summary>
        /// Gets or sets the DvLevel.
        /// </summary>
        /// <value>The DvLevel.</value>
        [JsonPropertyName("dv_level")]
        public int? DvLevel { get; set; }

        /// <summary>
        /// Gets or sets the RpuPresentFlag.
        /// </summary>
        /// <value>The RpuPresentFlag.</value>
        [JsonPropertyName("rpu_present_flag")]
        public int? RpuPresentFlag { get; set; }

        /// <summary>
        /// Gets or sets the ElPresentFlag.
        /// </summary>
        /// <value>The ElPresentFlag.</value>
        [JsonPropertyName("el_present_flag")]
        public int? ElPresentFlag { get; set; }

        /// <summary>
        /// Gets or sets the BlPresentFlag.
        /// </summary>
        /// <value>The BlPresentFlag.</value>
        [JsonPropertyName("bl_present_flag")]
        public int? BlPresentFlag { get; set; }

        /// <summary>
        /// Gets or sets the DvBlSignalCompatibilityId.
        /// </summary>
        /// <value>The DvBlSignalCompatibilityId.</value>
        [JsonPropertyName("dv_bl_signal_compatibility_id")]
        public int? DvBlSignalCompatibilityId { get; set; }

        /// <summary>
        /// Gets or sets the Rotation in degrees.
        /// </summary>
        /// <value>The Rotation.</value>
        [JsonPropertyName("rotation")]
        public int? Rotation { get; set; }
    }
}
