using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

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
        void SaveChapters(Guid itemId, IReadOnlyList<ChapterInfo> chapters);
    }
}
