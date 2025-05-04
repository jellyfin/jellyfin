using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Lyrics;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Lyrics;

/// <summary>
/// Interface ILyricsProvider.
/// </summary>
public interface ILyricProvider
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Search for lyrics.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of remote lyrics.</returns>
    Task<IEnumerable<RemoteLyricInfo>> SearchAsync(LyricSearchRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Get the lyrics.
    /// </summary>
    /// <param name="id">The remote lyric id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The lyric response.</returns>
    Task<LyricResponse?> GetLyricsAsync(string id, CancellationToken cancellationToken);
}
