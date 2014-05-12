using System.IO;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    /// <summary>
    /// Interface ISubtitleWriter
    /// </summary>
    public interface ISubtitleWriter
    {
        /// <summary>
        /// Writes the specified information.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="stream">The stream.</param>
        void Write(SubtitleTrackInfo info, Stream stream);
    }
}
