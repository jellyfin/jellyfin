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
        void SaveChapters(Guid itemId, IReadOnlyList<ChapterInfo> chapters);
    }
}
