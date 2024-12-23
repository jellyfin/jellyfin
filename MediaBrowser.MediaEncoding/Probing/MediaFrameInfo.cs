using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MediaBrowser.MediaEncoding.Probing;

/// <summary>
/// Class MediaFrameInfo.
/// </summary>
public class MediaFrameInfo
{
    /// <summary>
    /// Gets or sets the media type.
    /// </summary>
    [JsonPropertyName("media_type")]
    public string? MediaType { get; set; }

    /// <summary>
    /// Gets or sets the StreamIndex.
    /// </summary>
    [JsonPropertyName("stream_index")]
    public int? StreamIndex { get; set; }

    /// <summary>
    /// Gets or sets the KeyFrame.
    /// </summary>
    [JsonPropertyName("key_frame")]
    public int? KeyFrame { get; set; }

    /// <summary>
    /// Gets or sets the Pts.
    /// </summary>
    [JsonPropertyName("pts")]
    public long? Pts { get; set; }

    /// <summary>
    /// Gets or sets the PtsTime.
    /// </summary>
    [JsonPropertyName("pts_time")]
    public string? PtsTime { get; set; }

    /// <summary>
    /// Gets or sets the BestEffortTimestamp.
    /// </summary>
    [JsonPropertyName("best_effort_timestamp")]
    public long BestEffortTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the BestEffortTimestampTime.
    /// </summary>
    [JsonPropertyName("best_effort_timestamp_time")]
    public string? BestEffortTimestampTime { get; set; }

    /// <summary>
    /// Gets or sets the Duration.
    /// </summary>
    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    /// <summary>
    /// Gets or sets the DurationTime.
    /// </summary>
    [JsonPropertyName("duration_time")]
    public string? DurationTime { get; set; }

    /// <summary>
    /// Gets or sets the PktPos.
    /// </summary>
    [JsonPropertyName("pkt_pos")]
    public string? PktPos { get; set; }

    /// <summary>
    /// Gets or sets the PktSize.
    /// </summary>
    [JsonPropertyName("pkt_size")]
    public string? PktSize { get; set; }

    /// <summary>
    /// Gets or sets the Width.
    /// </summary>
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the Height.
    /// </summary>
    [JsonPropertyName("height")]
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the CropTop.
    /// </summary>
    [JsonPropertyName("crop_top")]
    public int? CropTop { get; set; }

    /// <summary>
    /// Gets or sets the CropBottom.
    /// </summary>
    [JsonPropertyName("crop_bottom")]
    public int? CropBottom { get; set; }

    /// <summary>
    /// Gets or sets the CropLeft.
    /// </summary>
    [JsonPropertyName("crop_left")]
    public int? CropLeft { get; set; }

    /// <summary>
    /// Gets or sets the CropRight.
    /// </summary>
    [JsonPropertyName("crop_right")]
    public int? CropRight { get; set; }

    /// <summary>
    /// Gets or sets the PixFmt.
    /// </summary>
    [JsonPropertyName("pix_fmt")]
    public string? PixFmt { get; set; }

    /// <summary>
    /// Gets or sets the SampleAspectRatio.
    /// </summary>
    [JsonPropertyName("sample_aspect_ratio")]
    public string? SampleAspectRatio { get; set; }

    /// <summary>
    /// Gets or sets the PictType.
    /// </summary>
    [JsonPropertyName("pict_type")]
    public string? PictType { get; set; }

    /// <summary>
    /// Gets or sets the InterlacedFrame.
    /// </summary>
    [JsonPropertyName("interlaced_frame")]
    public int? InterlacedFrame { get; set; }

    /// <summary>
    /// Gets or sets the TopFieldFirst.
    /// </summary>
    [JsonPropertyName("top_field_first")]
    public int? TopFieldFirst { get; set; }

    /// <summary>
    /// Gets or sets the RepeatPict.
    /// </summary>
    [JsonPropertyName("repeat_pict")]
    public int? RepeatPict { get; set; }

    /// <summary>
    /// Gets or sets the ColorRange.
    /// </summary>
    [JsonPropertyName("color_range")]
    public string? ColorRange { get; set; }

    /// <summary>
    /// Gets or sets the ColorSpace.
    /// </summary>
    [JsonPropertyName("color_space")]
    public string? ColorSpace { get; set; }

    /// <summary>
    /// Gets or sets the ColorPrimaries.
    /// </summary>
    [JsonPropertyName("color_primaries")]
    public string? ColorPrimaries { get; set; }

    /// <summary>
    /// Gets or sets the ColorTransfer.
    /// </summary>
    [JsonPropertyName("color_transfer")]
    public string? ColorTransfer { get; set; }

    /// <summary>
    /// Gets or sets the ChromaLocation.
    /// </summary>
    [JsonPropertyName("chroma_location")]
    public string? ChromaLocation { get; set; }

    /// <summary>
    /// Gets or sets the SideDataList.
    /// </summary>
    [JsonPropertyName("side_data_list")]
    public IReadOnlyList<MediaFrameSideDataInfo>? SideDataList { get; set; }
}
