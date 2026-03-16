namespace Jellyfin.Api.Helpers;

/// <summary>
/// Groups all parameters needed to build FFmpeg clip extraction arguments.
/// </summary>
internal sealed class ClipFfmpegOptions
{
    /// <summary>
    /// Gets or sets the absolute path to the source file.
    /// </summary>
    public required string InputPath { get; set; }

    /// <summary>
    /// Gets or sets the absolute path to the output file.
    /// </summary>
    public required string OutputPath { get; set; }

    /// <summary>
    /// Gets or sets the start offset in seconds.
    /// </summary>
    public double StartSeconds { get; set; }

    /// <summary>
    /// Gets or sets the duration in seconds.
    /// </summary>
    public double DurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the FFmpeg video encoder name (e.g. "libx264").
    /// </summary>
    public required string VideoEncoder { get; set; }

    /// <summary>
    /// Gets or sets the FFmpeg audio encoder name (e.g. "aac").
    /// </summary>
    public required string AudioEncoder { get; set; }

    /// <summary>
    /// Gets or sets the source video bitrate in bps, or <c>null</c> to let FFmpeg decide.
    /// </summary>
    public int? VideoBitRate { get; set; }

    /// <summary>
    /// Gets or sets the source audio bitrate in bps, or <c>null</c> to use default.
    /// </summary>
    public int? AudioBitRate { get; set; }

    /// <summary>
    /// Gets or sets the output container (e.g. "mp4", "webm").
    /// </summary>
    public required string Container { get; set; }

    /// <summary>
    /// Gets or sets the 0-based audio-only stream index to map.
    /// </summary>
    public int AudioStreamIndex { get; set; }
}
