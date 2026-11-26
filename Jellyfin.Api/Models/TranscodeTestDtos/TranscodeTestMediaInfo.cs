namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// The source of a stress test.
/// </summary>
public class TranscodeTestMediaInfo
{
    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

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
    public string? Codec { get; set; }

    /// <summary>
    /// Gets or sets the bit depth.
    /// </summary>
    public int? BitDepth { get; set; }

    /// <summary>
    /// Gets or sets the dynamic range type, for example HDR10 or DOVIWithHDR10.
    /// </summary>
    public string? VideoRangeType { get; set; }

    /// <summary>
    /// Gets or sets the overall bitrate, in megabits per second.
    /// </summary>
    public double? BitrateMbps { get; set; }

    /// <summary>
    /// Gets or sets the frame rate.
    /// </summary>
    public double? Fps { get; set; }
}
