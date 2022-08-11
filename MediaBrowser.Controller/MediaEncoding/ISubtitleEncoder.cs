#nullable disable

#pragma warning disable CS1591

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.MediaInfo;

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
        /// <param name="path">The path.</param>
        /// <param name="language">The language.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>System.String.</returns>
        Task<string> GetSubtitleFileCharacterSet(string path, string language, MediaProtocol protocol, CancellationToken cancellationToken);
    }
}
