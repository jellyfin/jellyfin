#nullable disable

#pragma warning disable CS1591

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.MediaEncoding
{
    public interface IEncodingManager
    {
        /// <summary>
        /// Refreshes the chapter images.
        /// </summary>
        /// <param name="video">Video to use.</param>
        /// <param name="directoryService">Directory service to use.</param>
        /// <param name="chapters">Set of chapters to refresh.</param>
        /// <param name="extractImages">Option to extract images.</param>
        /// <param name="saveChapters">Option to save chapters.</param>
        /// <param name="cancellationToken">CancellationToken to use for operation.</param>
        /// <returns><c>true</c> if successful, <c>false</c> if not.</returns>
        Task<bool> RefreshChapterImages(Video video, IDirectoryService directoryService, IReadOnlyList<ChapterInfo> chapters, bool extractImages, bool saveChapters, CancellationToken cancellationToken);
    }
}
