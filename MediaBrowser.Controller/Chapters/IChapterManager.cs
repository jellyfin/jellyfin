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
        /// Gets the configuration.
        /// </summary>
        /// <returns>ChapterOptions.</returns>
        ChapterOptions GetConfiguration();
    }
}
