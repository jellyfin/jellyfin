using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback
{
    public class EndlessStreamCopy
    {
        public async Task CopyStream(Stream source, Stream target, CancellationToken cancellationToken)
        {
            long position = 0;
            
            while (!cancellationToken.IsCancellationRequested)
            {
                await source.CopyToAsync(target, 81920, cancellationToken).ConfigureAwait(false);

                var fsPosition = source.Position;

                var bytesRead = fsPosition - position;

                //Logger.Debug("Streamed {0} bytes from file {1}", bytesRead, path);

                if (bytesRead == 0)
                {
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }

                position = fsPosition;
            }
        }
    }
}
