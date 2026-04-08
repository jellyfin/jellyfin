using System;
using System.Collections.Generic;

namespace Jellyfin.MediaEncoding.Hls.Playlist;

/// <summary>
/// Generator for dynamic HLS playlists where the segment lengths aren't known in advance.
/// </summary>
public interface IDynamicHlsPlaylistGenerator
{
    /// <summary>
    /// Creates the main playlist containing the main video or audio stream.
    /// </summary>
    /// <param name="request">An instance of the <see cref="CreateMainPlaylistRequest"/> class.</param>
    /// <returns>The playlist as a formatted string.</returns>
    string CreateMainPlaylist(CreateMainPlaylistRequest request);

    /// <summary>
    /// Gets the segment durations in seconds. When remuxing, uses keyframe-aligned boundaries
    /// when available. Otherwise, returns equal-length segments.
    /// </summary>
    /// <param name="mediaSourceId">The media source id.</param>
    /// <param name="filePath">The absolute file path.</param>
    /// <param name="desiredSegmentLengthMs">The desired segment length in milliseconds.</param>
    /// <param name="totalRuntimeTicks">The total runtime in ticks.</param>
    /// <param name="isRemuxing">Whether the video is being remuxed (copy codec).</param>
    /// <returns>The list of segment durations in seconds.</returns>
    IReadOnlyList<double> GetSegmentDurations(Guid? mediaSourceId, string filePath, int desiredSegmentLengthMs, long totalRuntimeTicks, bool isRemuxing);
}
