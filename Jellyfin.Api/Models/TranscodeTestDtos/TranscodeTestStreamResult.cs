namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// One stream of the capacity test.
/// </summary>
public class TranscodeTestStreamResult
{
    /// <summary>
    /// Gets or sets the stream number.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the frame rate over the last few seconds, while running.
    /// </summary>
    public double? Fps { get; set; }

    /// <summary>
    /// Gets or sets the frame rate when the test ended.
    /// </summary>
    public double? FinalFps { get; set; }

    /// <summary>
    /// Gets or sets the median frame rate.
    /// </summary>
    public double? MedianFps { get; set; }

    /// <summary>
    /// Gets or sets the output bitrate, in megabits per second.
    /// </summary>
    public double? OutputMbps { get; set; }

    /// <summary>
    /// Gets or sets the status.
    /// </summary>
    public TranscodeTestStreamStatus Status { get; set; }
}
