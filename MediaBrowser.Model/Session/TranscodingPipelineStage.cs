using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.Session;

/// <summary>
/// Class describing a single stage in the transcoding pipeline (for example a decoder, scaler, tone mapper or encoder).
/// </summary>
public class TranscodingPipelineStage
{
    /// <summary>
    /// Gets or sets the kind of operation this stage performs.
    /// </summary>
    public TranscodeStageType Type { get; set; }

    /// <summary>
    /// Gets or sets the media type of the chain this stage belongs to (<c>Video</c> or
    /// <c>Audio</c>), used to group/colour the pipeline lanes.
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// Gets or sets the hardware framework this stage runs on.
    /// </summary>
    public HardwareFramework Framework { get; set; }

    /// <summary>
    /// Gets or sets the underlying ffmpeg filter, decoder or encoder name (for example
    /// <c>hevc_qsv</c>, <c>vpp_qsv</c>, <c>tonemap_opencl</c> or <c>h264_qsv</c>).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets an optional human readable detail for the stage (for example the scaled
    /// resolution, the tone mapping algorithm or the codec long name).
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this stage is hardware accelerated.
    /// </summary>
    public bool IsHardware { get; set; }

    /// <summary>
    /// Gets or sets a label describing the frame data leaving this stage (for example
    /// <c>nv12 640x360</c>), shown on the connector to the next stage. Populated from the ffmpeg
    /// filter graph when available.
    /// </summary>
    public string? EdgeLabel { get; set; }

    /// <summary>
    /// Gets or sets the video bit depth of the frames at this stage (for example <c>8</c> or
    /// <c>10</c>). Only set on the video decode (source) and encode (output) stages.
    /// </summary>
    public int? VideoBitDepth { get; set; }

    /// <summary>
    /// Gets or sets the video range (<c>SDR</c>, <c>HDR10</c>, Dolby Vision, ...) of the frames at
    /// this stage. Only set on the video decode (source) and encode (output) stages; comparing the
    /// two reveals whether tone mapping (for example HDR10 -> SDR) takes place.
    /// </summary>
    public VideoRangeType? VideoRange { get; set; }

    /// <summary>
    /// Gets or sets the full Dolby Vision description of the frames at this stage, including the
    /// profile and its fallback (for example <c>Dolby Vision Profile 8.1 (HDR10)</c>). Only set
    /// where the Dolby Vision metadata survives: the decode (source) and passthrough stages. An
    /// encode drops the RPU, so it carries only a plain <see cref="VideoRange"/>.
    /// </summary>
    public string? VideoDoViTitle { get; set; }
}
