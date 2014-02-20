using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaEncoding
{
    public interface IEncodingManager
    {
        /// <summary>
        /// Gets the subtitle cache path.
        /// </summary>
        /// <param name="originalSubtitlePath">The original subtitle path.</param>
        /// <param name="outputSubtitleExtension">The output subtitle extension.</param>
        /// <returns>System.String.</returns>
        string GetSubtitleCachePath(string originalSubtitlePath, string outputSubtitleExtension);

        /// <summary>
        /// Gets the subtitle cache path.
        /// </summary>
        /// <param name="mediaPath">The media path.</param>
        /// <param name="subtitleStreamIndex">Index of the subtitle stream.</param>
        /// <param name="outputSubtitleExtension">The output subtitle extension.</param>
        /// <returns>System.String.</returns>
        string GetSubtitleCachePath(string mediaPath, int subtitleStreamIndex, string outputSubtitleExtension);

        /// <summary>
        /// Refreshes the chapter images.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        Task<bool> RefreshChapterImages(ChapterImageRefreshOptions options, CancellationToken cancellationToken);
    }
}
