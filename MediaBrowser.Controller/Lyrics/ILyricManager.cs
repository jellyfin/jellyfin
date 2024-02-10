using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Lyrics;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Lyrics;

/// <summary>
/// Interface ILyricManager.
/// </summary>
public interface ILyricManager
{
    /// <summary>
    /// Occurs when a lyric download fails.
    /// </summary>
    event EventHandler<LyricDownloadFailureEventArgs> LyricDownloadFailure;

    /// <summary>
    /// Search for lyrics for the specified song.
    /// </summary>
    /// <param name="audio">The song.</param>
    /// <param name="isAutomated">Whether the request is automated.</param>
    /// <param name="cancellationToken">CancellationToken to use for the operation.</param>
    /// <returns>The list of lyrics.</returns>
    Task<RemoteLyricInfo[]> SearchLyricsAsync(
        Audio audio,
        bool isAutomated,
        CancellationToken cancellationToken);

    /// <summary>
    /// Search for lyrics.
    /// </summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">CancellationToken to use for the operation.</param>
    /// <returns>The list of lyrics.</returns>
    Task<RemoteLyricInfo[]> SearchLyricsAsync(
        LyricSearchRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Download the lyrics.
    /// </summary>
    /// <param name="audio">The audio.</param>
    /// <param name="lyricId">The remote lyric id.</param>
    /// <param name="cancellationToken">CancellationToken to use for the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DownloadLyricsAsync(
        Audio audio,
        string lyricId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Download the lyrics.
    /// </summary>
    /// <param name="audio">The audio.</param>
    /// <param name="libraryOptions">The library options to use.</param>
    /// <param name="lyricId">The remote lyric id.</param>
    /// <param name="cancellationToken">CancellationToken to use for the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DownloadLyricsAsync(
        Audio audio,
        LibraryOptions libraryOptions,
        string lyricId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Upload new lyrics.
    /// </summary>
    /// <param name="audio">The audio file the lyrics belong to.</param>
    /// <param name="lyricResponse">The lyric response.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task UploadLyricAsync(Audio audio, LyricResponse lyricResponse);

    /// <summary>
    /// Get the remote lyrics.
    /// </summary>
    /// <param name="id">The remote lyrics id.</param>
    /// <param name="cancellationToken">CancellationToken to use for the operation.</param>
    /// <returns>The lyric response.</returns>
    Task<LyricResponse> GetRemoteLyricsAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the lyrics.
    /// </summary>
    /// <param name="audio">The audio file to remove lyrics from.</param>
    /// <param name="index">The lyric index.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeleteLyricsAsync(Audio audio, int index);

    /// <summary>
    /// Get the list of lyric providers.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>Lyric providers.</returns>
    IReadOnlyList<LyricProviderInfo> GetSupportedProviders(BaseItem item);

    /// <summary>
    /// Get the existing lyric for the audio.
    /// </summary>
    /// <param name="audio">The audio item.</param>
    /// <param name="streamIndex">The stream index.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed lyric model.</returns>
    Task<LyricModel?> GetLyricsAsync(Audio audio, int streamIndex, CancellationToken cancellationToken);
}
