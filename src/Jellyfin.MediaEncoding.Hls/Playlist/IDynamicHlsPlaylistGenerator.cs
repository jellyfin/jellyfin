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
}
