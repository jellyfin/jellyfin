using System.Threading;
using System.Threading.Tasks;

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
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The playlist as a formatted string.</returns>
    Task<string> CreateMainPlaylistAsync(CreateMainPlaylistRequest request, CancellationToken cancellationToken);
}
