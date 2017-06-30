using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Subtitles
{
    public interface ISubtitleManager
    {
        /// <summary>
        /// Occurs when [subtitle download failure].
        /// </summary>
        event EventHandler<SubtitleDownloadFailureEventArgs> SubtitleDownloadFailure;

        /// <summary>
        /// Occurs when [subtitles downloaded].
        /// </summary>
        event EventHandler<SubtitleDownloadEventArgs> SubtitlesDownloaded;

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="subtitleProviders">The subtitle providers.</param>
        void AddParts(IEnumerable<ISubtitleProvider> subtitleProviders);

        /// <summary>
        /// Searches the subtitles.
        /// </summary>
        Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitles(Video video,
            string language,
            bool? isPerfectMatch,
            CancellationToken cancellationToken);

        /// <summary>
        /// Searches the subtitles.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RemoteSubtitleInfo}}.</returns>
        Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitles(SubtitleSearchRequest request,
            CancellationToken cancellationToken);

        /// <summary>
        /// Downloads the subtitles.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="subtitleId">The subtitle identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task DownloadSubtitles(Video video,
            string subtitleId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the remote subtitles.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SubtitleResponse}.</returns>
        Task<SubtitleResponse> GetRemoteSubtitles(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the subtitles.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="index">The index.</param>
        /// <returns>Task.</returns>
        Task DeleteSubtitles(string itemId, int index);

        /// <summary>
        /// Gets the providers.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>IEnumerable{SubtitleProviderInfo}.</returns>
        IEnumerable<SubtitleProviderInfo> GetProviders(string itemId);
    }
}
