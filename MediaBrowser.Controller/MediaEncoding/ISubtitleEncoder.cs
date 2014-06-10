using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaEncoding
{
    public interface ISubtitleEncoder
    {
        Task<Stream> ConvertTextSubtitle(String stream, 
            string inputFormat, 
            string outputFormat,
            CancellationToken cancellationToken);
    }
}
