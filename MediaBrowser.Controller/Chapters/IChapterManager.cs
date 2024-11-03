using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
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

        /// <summary>
        /// Adds external chapters.
        /// </summary>
        /// <param name="video">The video item.</param>
        /// <param name="chapters">The set of chapters.</param>
        /// <returns>A list of ChapterInfo objects created using data read from the local XML file.</returns>
        public ChapterInfo[] AddExternalChapters(Video video, IReadOnlyList<ChapterInfo> chapters);
    }
}
