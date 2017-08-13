using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.MediaEncoding
{
    public interface IEncodingManager
    {
        /// <summary>
        /// Refreshes the chapter images.
        /// </summary>
        Task<bool> RefreshChapterImages(Video video, List<ChapterInfo> chapters, bool extractImages, bool saveChapters, CancellationToken cancellationToken);
    }
}
