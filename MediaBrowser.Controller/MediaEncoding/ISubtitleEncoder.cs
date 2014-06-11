using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaEncoding
{
    public interface ISubtitleEncoder
    {
        Task<Stream> ConvertSubtitles(
            Stream stream, 
            string inputFormat, 
            string outputFormat,
            CancellationToken cancellationToken);

        Task<Stream> GetSubtitles(string itemId, 
            string mediaSourceId,
            int subtitleStreamIndex,
            string outputFormat,
            CancellationToken cancellationToken);
    }
}
