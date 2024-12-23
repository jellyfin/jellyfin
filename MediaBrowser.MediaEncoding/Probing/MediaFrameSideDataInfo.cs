using System.Text.Json.Serialization;

namespace MediaBrowser.MediaEncoding.Probing;

/// <summary>
/// Class MediaFrameSideDataInfo.
/// Currently only records the SideDataType for HDR10+ detection.
/// </summary>
public class MediaFrameSideDataInfo
{
    /// <summary>
    /// Gets or sets the SideDataType.
    /// </summary>
    [JsonPropertyName("side_data_type")]
    public string? SideDataType { get; set; }
}
