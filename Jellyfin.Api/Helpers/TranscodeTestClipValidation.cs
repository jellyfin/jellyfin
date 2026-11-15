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
}
