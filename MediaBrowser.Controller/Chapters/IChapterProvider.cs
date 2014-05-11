using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Chapters;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Chapters
{
    public interface IChapterProvider
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
        /// Searches the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RemoteChapterResult}}.</returns>
        Task<IEnumerable<RemoteChapterResult>> Search(ChapterSearchRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the chapters.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ChapterResponse}.</returns>
        Task<ChapterResponse> GetChapters(string id, CancellationToken cancellationToken);
    }
}
