using System;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Checks that a probeable cache entry still describes the complete clip the compatibility test generated.
/// </summary>
internal static class TranscodeTestClipValidation
{
    internal const int ClipFrames = 96;
    private const long ClipDurationTicks = 4 * TimeSpan.TicksPerSecond;
    private const long DurationToleranceTicks = TimeSpan.TicksPerSecond / 24;

    /// <summary>
    /// Validates the clip's duration and video format, rather than accepting any non-empty, probeable video.
    /// </summary>
    /// <param name="info">The probed media information.</param>
    /// <param name="codec">The codec the test generated.</param>
    /// <param name="bitDepth">The generated bit depth.</param>
    /// <param name="hdr">Whether the generated clip was tagged as HDR10.</param>
    /// <returns>Whether the cache entry matches the generated clip.</returns>
    internal static bool IsValid(MediaSourceInfo info, string codec, int bitDepth, bool hdr)
    {
        if (info.RunTimeTicks is not long ticks
            || ticks < ClipDurationTicks - DurationToleranceTicks
            || ticks > ClipDurationTicks + DurationToleranceTicks)
        {
            return false;
        }

        var video = info.MediaStreams.Where(stream => stream.Type == MediaStreamType.Video).ToList();
        if (video.Count != 1)
        {
            return false;
        }

        var stream = video[0];
        var pixelFormat = bitDepth switch
        {
            8 => "yuv420p",
            10 => "yuv420p10le",
            _ => null
        };
        if (pixelFormat is null
            || !string.Equals(stream.Codec, codec, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(stream.PixelFormat, pixelFormat, StringComparison.OrdinalIgnoreCase)
            || (stream.BitDepth is int reportedDepth && reportedDepth != bitDepth))
        {
            return false;
        }

        return hdr
            ? stream.VideoRangeType == VideoRangeType.HDR10
                && string.Equals(stream.ColorTransfer, "smpte2084", StringComparison.OrdinalIgnoreCase)
                && string.Equals(stream.ColorPrimaries, "bt2020", StringComparison.OrdinalIgnoreCase)
                && string.Equals(stream.ColorSpace, "bt2020nc", StringComparison.OrdinalIgnoreCase)
            : stream.VideoRangeType == VideoRangeType.SDR;
    }

    /// <summary>
    /// Verifies the clip's actual frames using an independent software decode, not a hardware test's outcome.
    /// </summary>
    /// <param name="exitCode">The software decoder's exit code.</param>
    /// <param name="frames">The frames it produced.</param>
    /// <param name="output">The software decoder's diagnostic output.</param>
    /// <returns>True for a complete clip, false for a proven invalid clip, or null when verification failed.</returns>
    internal static bool? IsCompleteSoftwareDecode(int exitCode, int? frames, string? output)
    {
        // A format header may retain the original duration even after its packets have been truncated.
        if (output is not null
            && (output.Contains("File ended prematurely", StringComparison.OrdinalIgnoreCase)
                || output.Contains("Invalid data found when processing input", StringComparison.OrdinalIgnoreCase)
                || output.Contains("corrupt decoded frame", StringComparison.OrdinalIgnoreCase)
                || output.Contains("Packet corrupt", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Only a finished software decode proves that a short frame count is the source's end. A timeout,
        // unavailable decoder, or resource failure must not delete a potentially good cached clip.
        return exitCode == 0 && frames is int count ? count == ClipFrames : null;
    }
}
