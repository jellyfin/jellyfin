using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaEncoding
{
    public interface IEncodingManager
    {
        /// <summary>
        /// Refreshes the chapter images.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        Task<bool> RefreshChapterImages(ChapterImageRefreshOptions options, CancellationToken cancellationToken);
    }
}
