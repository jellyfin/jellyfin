using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Chapters
{
    /// <summary>
    /// Interface IChapterManager.
    /// </summary>
    public interface IChapterManager
    {
        /// <summary>
        /// Saves the chapters.
        /// </summary>
        /// <param name="itemId">The item.</param>
        /// <param name="chapters">The set of chapters.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task SaveChapters(Guid itemId, IReadOnlyList<ChapterInfo> chapters);

        /// <summary>
        /// Get a chapter for item and index.
        /// </summary>
        /// <param name="item">The BaseItem.</param>
        /// <param name="chapterIndex">The ChapterIndex.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="ChapterInfo"/>.</returns>
        public Task<ChapterInfo?> GetChapter(BaseItem item, int chapterIndex, CancellationToken cancellationToken);

        /// <summary>
        /// Get all chapters for item.
        /// </summary>
        /// <param name="item">The BaseItem.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="ChapterInfo"/>.</returns>
        public Task<List<ChapterInfo>> GetChapters(BaseItem item, CancellationToken cancellationToken);
    }
}
