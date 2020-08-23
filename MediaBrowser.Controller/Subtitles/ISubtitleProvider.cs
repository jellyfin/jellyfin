#pragma warning disable CS1591

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Subtitles
{
    public interface ISubtitleProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the supported media types.
        /// </summary>
        /// <value>The supported media types.</value>
        IEnumerable<VideoContentType> SupportedMediaTypes { get; }

        /// <summary>
        /// Searches the subtitles.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RemoteSubtitleInfo}}.</returns>
        Task<IEnumerable<RemoteSubtitleInfo>> Search(SubtitleSearchRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the subtitles.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SubtitleResponse}.</returns>
        Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken);
    }
}
