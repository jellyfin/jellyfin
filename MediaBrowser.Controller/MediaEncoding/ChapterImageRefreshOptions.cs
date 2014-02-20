using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.MediaEncoding
{
    public class ChapterImageRefreshOptions
    {
        public Video Video { get; set; }

        public List<ChapterInfo> Chapters { get; set; }

        public bool SaveChapters { get; set; }

        public bool ExtractImages { get; set; }
    }
}
