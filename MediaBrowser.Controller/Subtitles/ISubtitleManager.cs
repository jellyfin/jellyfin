using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Providers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Subtitles
{
    public interface ISubtitleManager
    {
        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="subtitleProviders">The subtitle providers.</param>
        void AddParts(IEnumerable<ISubtitleProvider> subtitleProviders);

        /// <summary>
        /// Searches the subtitles.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="language">The language.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RemoteSubtitleInfo}}.</returns>
        Task<IEnumerable<RemoteSubtitleInfo>> SearchSubtitles(Video video,
            string language,
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
        /// <param name="providerName">Name of the provider.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task DownloadSubtitles(Video video, 
            string subtitleId, 
            string providerName, 
            CancellationToken cancellationToken);
    }
}
