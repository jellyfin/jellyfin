#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Subtitles
{
    public interface ISubtitleManager
    {
        /// <summary>
        /// Occurs when [subtitle download failure].
        /// </summary>
        event EventHandler<SubtitleDownloadFailureEventArgs> SubtitleDownloadFailure;

        /// <summary>
        /// Searches the subtitles.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="language">Subtitle language.</param>
        /// <param name="isPerfectMatch">Require perfect match.</param>
        /// <param name="isAutomated">Request is automated.</param>
        /// <param name="cancellationToken">CancellationToken to use for the operation.</param>
        /// <returns>Subtitles, wrapped in task.</returns>
        Task<RemoteSubtitleInfo[]> SearchSubtitles(
            Video video,
            string language,
            bool? isPerfectMatch,
            bool isAutomated,
            CancellationToken cancellationToken);

        /// <summary>
        /// Searches the subtitles.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{RemoteSubtitleInfo[]}.</returns>
        Task<RemoteSubtitleInfo[]> SearchSubtitles(
            SubtitleSearchRequest request,
            CancellationToken cancellationToken);

        /// <summary>
        /// Downloads the subtitles.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="subtitleId">Subtitle ID.</param>
        /// <param name="cancellationToken">CancellationToken to use for the operation.</param>
        /// <returns>A task.</returns>
        Task DownloadSubtitles(Video video, string subtitleId, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads the subtitles.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="libraryOptions">Library options to use.</param>
        /// <param name="subtitleId">Subtitle ID.</param>
        /// <param name="cancellationToken">CancellationToken to use for the operation.</param>
        /// <returns>A task.</returns>
        Task DownloadSubtitles(Video video, LibraryOptions libraryOptions, string subtitleId, CancellationToken cancellationToken);

        /// <summary>
        /// Upload new subtitle.
        /// </summary>
        /// <param name="video">The video the subtitle belongs to.</param>
        /// <param name="response">The subtitle response.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task UploadSubtitle(Video video, SubtitleResponse response);

        /// <summary>
        /// Gets the remote subtitles.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><see cref="Task{SubtitleResponse}" />.</returns>
        Task<SubtitleResponse> GetRemoteSubtitles(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the subtitles.
        /// </summary>
        /// <param name="item">Media item.</param>
        /// <param name="index">Subtitle index.</param>
        /// <returns>A task.</returns>
        Task DeleteSubtitles(BaseItem item, int index);

        /// <summary>
        /// Gets the providers.
        /// </summary>
        /// <param name="item">The media item.</param>
        /// <returns>Subtitles providers.</returns>
        SubtitleProviderInfo[] GetSupportedProviders(BaseItem item);
    }
}
