using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Bounds subtitle conversion to MP4-compatible text or an explicit permanent burn-in.
/// </summary>
public static class OfflineDownloadTracks
{
    /// <summary>Whether this stream can be converted to MP4 timed text.</summary>
    /// <param name="stream">The selected subtitle metadata.</param>
    /// <returns>True for supported text subtitles.</returns>
    public static bool CanEmbed(MediaStream stream)
        => stream.Type == MediaStreamType.Subtitle && (stream.Codec?.ToLowerInvariant() is "srt" or "subrip" or "ass" or "ssa" or "webvtt" or "vtt" or "text" or "mov_text");

    /// <summary>Whether the standard pipeline supports burning this subtitle into video.</summary>
    /// <param name="stream">The selected subtitle metadata.</param>
    /// <returns>True for supported text or bitmap formats.</returns>
    public static bool CanBurn(MediaStream stream)
        => CanEmbed(stream) || (stream.Type == MediaStreamType.Subtitle && (stream.Codec?.ToLowerInvariant() is "pgssub" or "hdmv_pgs_subtitle" or "dvdsub" or "dvd_subtitle" or "dvbsub" or "dvb_subtitle"));

    /// <summary>Validates an explicit subtitle selection against server metadata, never client paths/codecs.</summary>
    /// <param name="streams">Available streams.</param>
    /// <param name="index">Selected subtitle index.</param>
    /// <param name="mode">none, embed, or burn.</param>
    /// <returns>A safe error message, or null if valid.</returns>
    public static string? Validate(IEnumerable<MediaStream> streams, int? index, string mode)
    {
        if (mode == "none")
        {
            return index is null or -1 ? null : "Choose a subtitle mode for the selected track.";
        }

        if (mode is not ("embed" or "burn"))
        {
            return "Unsupported subtitle mode.";
        }

        var track = streams.FirstOrDefault(stream => stream.Type == MediaStreamType.Subtitle && stream.Index == index);
        if (track is null)
        {
            return "The selected subtitle track is not available.";
        }

        return (mode == "embed" ? CanEmbed(track) : CanBurn(track)) ? null : "This subtitle format does not support the selected mode.";
    }
}
