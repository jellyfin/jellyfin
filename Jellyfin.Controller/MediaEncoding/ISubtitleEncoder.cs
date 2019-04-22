using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Controller.Entities;
using Jellyfin.Model.MediaInfo;

namespace Jellyfin.Controller.MediaEncoding
{
    public interface ISubtitleEncoder
    {
        /// <summary>
        /// Gets the subtitles.
        /// </summary>
        /// <returns>Task{Stream}.</returns>
        Task<Stream> GetSubtitles(BaseItem item,
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
        /// <param name="protocol">The protocol.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>System.String.</returns>
        Task<string> GetSubtitleFileCharacterSet(string path, string language, MediaProtocol protocol, CancellationToken cancellationToken);
    }
}
