using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

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
    /// Gets the length of every segment of the main playlist when the playlist is cut at the keyframes of the file.
    /// </summary>
    /// <param name="request">An instance of the <see cref="CreateMainPlaylistRequest"/> class.</param>
    /// <param name="segmentLengths">The segment lengths in seconds, in playlist order.</param>
    /// <returns><c>true</c> if the playlist is cut at keyframes, <c>false</c> if it uses equal lengths because the video is transcoded or has no keyframe data.</returns>
    bool TryGetKeyframeSegmentLengths(CreateMainPlaylistRequest request, [NotNullWhen(true)] out IReadOnlyList<double>? segmentLengths);
}
