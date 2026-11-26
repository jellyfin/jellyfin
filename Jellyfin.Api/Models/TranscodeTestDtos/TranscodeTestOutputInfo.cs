namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// What each stress test stream produces.
/// </summary>
public class TranscodeTestOutputInfo
{
    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the video codec.
    /// </summary>
    public string Codec { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target video bitrate, in megabits per second.
    /// </summary>
    public double BitrateMbps { get; set; }

    /// <summary>
    /// Gets or sets the audio codec.
    /// </summary>
    public string? AudioCodec { get; set; }

    /// <summary>
    /// Gets or sets the audio channels.
    /// </summary>
    public int? AudioChannels { get; set; }

    /// <summary>
    /// Gets or sets the ffmpeg video encoder.
    /// </summary>
    public string Encoder { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the source is decoded on the GPU.
    /// </summary>
    public bool HardwareDecoding { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the output is encoded on the GPU.
    /// </summary>
    public bool HardwareEncoding { get; set; }

    /// <summary>
    /// Gets or sets how HDR is tone-mapped.
    /// </summary>
    public TranscodeTestTonemapMode Tonemap { get; set; }
}
