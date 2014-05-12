using MediaBrowser.Model.Chapters;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Chapters
{
    public class ChapterResponse
    {
        /// <summary>
        /// Gets or sets the chapters.
        /// </summary>
        /// <value>The chapters.</value>
        public List<RemoteChapterInfo> Chapters { get; set; }

        public ChapterResponse()
        {
            Chapters = new List<RemoteChapterInfo>();
        }
    }
}