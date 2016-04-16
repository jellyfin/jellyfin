using MediaBrowser.Model.MediaInfo;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaEncoding
{
    public interface ISubtitleEncoder
    {
        /// <summary>
        /// Gets the subtitles.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="mediaSourceId">The media source identifier.</param>
        /// <param name="subtitleStreamIndex">Index of the subtitle stream.</param>
        /// <param name="outputFormat">The output format.</param>
        /// <param name="startTimeTicks">The start time ticks.</param>
        /// <param name="endTimeTicks">The end time ticks.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<Stream> GetSubtitles(string itemId,
            string mediaSourceId,
            int subtitleStreamIndex,
            string outputFormat,
            long startTimeTicks,
            long? endTimeTicks,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the subtitle language encoding parameter.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>System.String.</returns>
        Task<string> GetSubtitleFileCharacterSet(string path, string language, MediaProtocol protocol, CancellationToken cancellationToken);
    }
}
