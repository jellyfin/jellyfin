using System.Collections.Generic;
using System.Diagnostics;

namespace MediaBrowser.Model.Configuration;

/// <summary>
/// Class TrickplayOptions.
/// </summary>
public class TrickplayOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether or not to use HW acceleration.
    /// </summary>
    public bool EnableHwAcceleration { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether or not to use HW accelerated MJPEG encoding.
    /// </summary>
    public bool EnableHwEncoding { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to only extract key frames.
    /// Significantly faster, but is not compatible with all decoders and/or video files.
    /// </summary>
    public bool EnableKeyFrameOnlyExtraction { get; set; } = false;

    /// <summary>
    /// Gets or sets the behavior used by trickplay provider on library scan/update.
    /// </summary>
    public TrickplayScanBehavior ScanBehavior { get; set; } = TrickplayScanBehavior.NonBlocking;

    /// <summary>
    /// Gets or sets the process priority for the ffmpeg process.
    /// </summary>
    public ProcessPriorityClass ProcessPriority { get; set; } = ProcessPriorityClass.BelowNormal;

    /// <summary>
    /// Gets or sets the interval, in ms, between each new trickplay image.
    /// </summary>
    public int Interval { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the target width resolutions, in px, to generates preview images for.
    /// </summary>
    public int[] WidthResolutions { get; set; } = new[] { 320 };

    /// <summary>
    /// Gets or sets number of tile images to allow in X dimension.
    /// </summary>
    public int TileWidth { get; set; } = 10;

    /// <summary>
    /// Gets or sets number of tile images to allow in Y dimension.
    /// </summary>
    public int TileHeight { get; set; } = 10;

    /// <summary>
    /// Gets or sets the ffmpeg output quality level.
    /// </summary>
    public int Qscale { get; set; } = 4;

    /// <summary>
    /// Gets or sets the jpeg quality to use for image tiles.
    /// </summary>
    public int JpegQuality { get; set; } = 90;

    /// <summary>
    /// Gets or sets the number of threads to be used by ffmpeg.
    /// </summary>
    public int ProcessThreads { get; set; } = 1;
}
