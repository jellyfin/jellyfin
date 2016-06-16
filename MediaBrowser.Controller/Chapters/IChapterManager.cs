using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Chapters;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Chapters
{
    /// <summary>
    /// Interface IChapterManager
    /// </summary>
    public interface IChapterManager
    {
        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="chapterProviders">The chapter providers.</param>
        void AddParts(IEnumerable<IChapterProvider> chapterProviders);

        /// <summary>
        /// Gets the chapters.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>List{ChapterInfo}.</returns>
        IEnumerable<ChapterInfo> GetChapters(string itemId);

        /// <summary>
        /// Saves the chapters.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="chapters">The chapters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveChapters(string itemId, List<ChapterInfo> chapters, CancellationToken cancellationToken);

        /// <summary>
        /// Searches the specified video.
        /// </summary>
        /// <param name="video">The video.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RemoteChapterResult}}.</returns>
        Task<IEnumerable<RemoteChapterResult>> Search(Video video, CancellationToken cancellationToken);

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

        /// <summary>
        /// Gets the providers.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>IEnumerable{ChapterProviderInfo}.</returns>
        IEnumerable<ChapterProviderInfo> GetProviders(string itemId);

        /// <summary>
        /// Gets the providers.
        /// </summary>
        /// <returns>IEnumerable{ChapterProviderInfo}.</returns>
        IEnumerable<ChapterProviderInfo> GetProviders();

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <returns>ChapterOptions.</returns>
        ChapterOptions GetConfiguration();
    }
}
