using System.IO;
using System.Threading;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// Interface ISubtitleWriter.
    /// </summary>
    public interface ISubtitleWriter
    {
        /// <summary>
        /// Writes the specified information.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="stream">The stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken);
    }
}
