using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;

namespace Jellyfin.Controller.MediaEncoding
{
    public interface IEncodingManager
    {
        /// <summary>
        /// Refreshes the chapter images.
        /// </summary>
        Task<bool> RefreshChapterImages(Video video, IDirectoryService directoryService, List<ChapterInfo> chapters, bool extractImages, bool saveChapters, CancellationToken cancellationToken);
    }
}
