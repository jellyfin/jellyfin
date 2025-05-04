#nullable disable

#pragma warning disable CS1591

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.MediaEncoding
{
    public interface ISubtitleEncoder
    {
        /// <summary>
        /// Gets the subtitles.
        /// </summary>
        /// <param name="item">Item to use.</param>
        /// <param name="mediaSourceId">Media source.</param>
        /// <param name="subtitleStreamIndex">Subtitle stream to use.</param>
        /// <param name="outputFormat">Output format to use.</param>
        /// <param name="startTimeTicks">Start time.</param>
        /// <param name="endTimeTicks">End time.</param>
        /// <param name="preserveOriginalTimestamps">Option to preserve original timestamps.</param>
        /// <param name="cancellationToken">The cancellation token for the operation.</param>
        /// <returns>Task{Stream}.</returns>
        Task<Stream> GetSubtitles(
            BaseItem item,
            string mediaSourceId,
            int subtitleStreamIndex,
            string outputFormat,
            long startTimeTicks,
            long endTimeTicks,
            bool preserveOriginalTimestamps,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the subtitle language encoding parameter.
        /// </summary>
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <param name="language">The language.</param>
        /// <param name="mediaSource">The media source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>System.String.</returns>
        Task<string> GetSubtitleFileCharacterSet(MediaStream subtitleStream, string language, MediaSourceInfo mediaSource, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the path to a subtitle file.
        /// </summary>
        /// <param name="subtitleStream">The subtitle stream.</param>
        /// <param name="mediaSource">The media source.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>System.String.</returns>
        Task<string> GetSubtitleFilePath(MediaStream subtitleStream, MediaSourceInfo mediaSource, CancellationToken cancellationToken);
    }
}
