#pragma warning disable CS1591

using System.IO;
using System.Threading;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public interface ISubtitleParser
    {
        /// <summary>
        /// Parses the specified stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>SubtitleTrackInfo.</returns>
        SubtitleTrackInfo Parse(Stream stream, CancellationToken cancellationToken);
    }
}
