#nullable disable

using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Session;

/// <summary>
/// Class holding information on a running transcode.
/// </summary>
public class TranscodingInfo
{
    /// <summary>
    /// Gets or sets the thread count used for encoding.
    /// </summary>
    public string AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the thread count used for encoding.
    /// </summary>
    public string VideoCodec { get; set; }

    /// <summary>
    /// Gets or sets the thread count used for encoding.
    /// </summary>
    public string Container { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the video is passed through.
    /// </summary>
    public bool IsVideoDirect { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the audio is passed through.
    /// </summary>
    public bool IsAudioDirect { get; set; }

    /// <summary>
    /// Gets or sets the bitrate.
    /// </summary>
    public int? Bitrate { get; set; }

    /// <summary>
    /// Gets or sets the framerate.
    /// </summary>
    public float? Framerate { get; set; }

    /// <summary>
    /// Gets or sets the completion percentage.
    /// </summary>
    public double? CompletionPercentage { get; set; }

    /// <summary>
    /// Gets or sets the video width.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the video height.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the audio channels.
    /// </summary>
    public int? AudioChannels { get; set; }

    /// <summary>
    /// Gets or sets the hardware acceleration type.
    /// </summary>
    public HardwareAccelerationType? HardwareAccelerationType { get; set; }

    /// <summary>
    /// Gets or sets the transcode reasons.
    /// </summary>
    public TranscodeReason TranscodeReasons { get; set; }
}
