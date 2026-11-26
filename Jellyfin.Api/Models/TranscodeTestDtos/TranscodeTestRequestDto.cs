using System;
using System.ComponentModel.DataAnnotations;

namespace Jellyfin.Api.Models.TranscodeTestDtos;

/// <summary>
/// What a transcoding test should run.
/// </summary>
public class TranscodeTestRequestDto
{
    /// <summary>
    /// The lowest stress test video bitrate, in bits per second.
    /// </summary>
    public const int MinVideoBitrate = 250_000;

    /// <summary>
    /// The highest stress test video bitrate, in bits per second. Far above what the dashboard offers, and low enough
    /// that a long test with many streams cannot fill the transcode folder.
    /// </summary>
    public const int MaxVideoBitrate = 100_000_000;

    /// <summary>
    /// Gets or sets the kind of test.
    /// </summary>
    [EnumDataType(typeof(TranscodeTestKind))]
    public TranscodeTestKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the library item for the stress test.
    /// </summary>
    public Guid? ItemId { get; set; }

    /// <summary>
    /// Gets or sets the media source of the item, or <c>null</c> for the first one.
    /// </summary>
    public string? MediaSourceId { get; set; }

    /// <summary>
    /// Gets or sets the output video codec of the stress test: h264, hevc or av1.
    /// </summary>
    public string VideoCodec { get; set; } = "h264";

    /// <summary>
    /// Gets or sets the maximum output width of the stress test.
    /// </summary>
    [Range(320, 7680)]
    public int MaxWidth { get; set; } = 1920;

    /// <summary>
    /// Gets or sets the output video bitrate of the stress test, in bits per second.
    /// </summary>
    [Range(MinVideoBitrate, MaxVideoBitrate)]
    public int VideoBitrate { get; set; } = 8_000_000;

    /// <summary>
    /// Gets or sets the most streams the stress test may run at once.
    /// </summary>
    [Range(1, 64)]
    public int MaxStreams { get; set; } = 16;
}
