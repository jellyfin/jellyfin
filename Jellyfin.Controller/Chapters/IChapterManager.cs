using System.Collections.Generic;
using Jellyfin.Model.Entities;

namespace Jellyfin.Controller.Chapters
{
    /// <summary>
    /// Interface IChapterManager
    /// </summary>
    public interface IChapterManager
    {
        /// <summary>
        /// Saves the chapters.
        /// </summary>
        void SaveChapters(string itemId, List<ChapterInfo> chapters);
    }
}
