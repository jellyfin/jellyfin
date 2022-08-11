#pragma warning disable CS1591

using System.IO;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public interface ISubtitleParser
    {
        /// <summary>
        /// Parses the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns>SubtitleTrackInfo.</returns>
        SubtitleTrackInfo Parse(Stream stream, string fileExtension);

        /// <summary>
        /// Determines whether the file extension is supported by the parser.
        /// </summary>
        /// <param name="fileExtension">The file extension.</param>
        /// <returns>A value indicating whether the file extension is supported.</returns>
        bool SupportsFileExtension(string fileExtension);
    }
}
