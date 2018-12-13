using System.IO;
using System.Threading;
using System;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.IO
{
    public class StreamHelper : IStreamHelper
    {
        public async Task CopyToAsync(Stream source, Stream destination, int bufferSize, Action onStarted, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                await destination.WriteAsync(buffer, 0, read).ConfigureAwait(false);

                if (onStarted != null)
                {
                    onStarted();
                    onStarted = null;
                }
            }
        }

        public async Task CopyToAsync(Stream source, Stream destination, int bufferSize, int emptyReadLimit, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[bufferSize];

            if (emptyReadLimit <= 0)
            {
                int read;
                while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await destination.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                }

                return;
            }

            var eofCount = 0;

            while (eofCount < emptyReadLimit)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bytesRead = source.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    eofCount++;
                    await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    eofCount = 0;

                    await destination.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                }
            }
        }

        const int StreamCopyToBufferSize = 81920;
        public async Task<int> CopyToAsync(Stream source, Stream destination, CancellationToken cancellationToken)
        {
            var array = new byte[StreamCopyToBufferSize];
            int bytesRead;
            int totalBytesRead = 0;

            while ((bytesRead = await source.ReadAsync(array, 0, array.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                var bytesToWrite = bytesRead;

                if (bytesToWrite > 0)
                {
                    await destination.WriteAsync(array, 0, Convert.ToInt32(bytesToWrite), cancellationToken).ConfigureAwait(false);

                    totalBytesRead += bytesRead;
                }
            }

            return totalBytesRead;
        }

        public async Task<int> CopyToAsyncWithSyncRead(Stream source, Stream destination, CancellationToken cancellationToken)
        {
            var array = new byte[StreamCopyToBufferSize];
            int bytesRead;
            int totalBytesRead = 0;

            while ((bytesRead = source.Read(array, 0, array.Length)) != 0)
            {
                var bytesToWrite = bytesRead;

                if (bytesToWrite > 0)
                {
                    await destination.WriteAsync(array, 0, Convert.ToInt32(bytesToWrite), cancellationToken).ConfigureAwait(false);

                    totalBytesRead += bytesRead;
                }
            }

            return totalBytesRead;
        }

        public async Task CopyToAsyncWithSyncRead(Stream source, Stream destination, long copyLength, CancellationToken cancellationToken)
        {
            var array = new byte[StreamCopyToBufferSize];
            int bytesRead;

            while ((bytesRead = source.Read(array, 0, array.Length)) != 0)
            {
                var bytesToWrite = Math.Min(bytesRead, copyLength);

                if (bytesToWrite > 0)
                {
                    await destination.WriteAsync(array, 0, Convert.ToInt32(bytesToWrite), cancellationToken).ConfigureAwait(false);
                }

                copyLength -= bytesToWrite;

                if (copyLength <= 0)
                {
                    break;
                }
            }
        }

        public async Task CopyToAsync(Stream source, Stream destination, long copyLength, CancellationToken cancellationToken)
        {
            var array = new byte[StreamCopyToBufferSize];
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(array, 0, array.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                var bytesToWrite = Math.Min(bytesRead, copyLength);

                if (bytesToWrite > 0)
                {
                    await destination.WriteAsync(array, 0, Convert.ToInt32(bytesToWrite), cancellationToken).ConfigureAwait(false);
                }

                copyLength -= bytesToWrite;

                if (copyLength <= 0)
                {
                    break;
                }
            }
        }

        public async Task CopyUntilCancelled(Stream source, Stream target, int bufferSize, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[bufferSize];

            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await CopyToAsyncInternal(source, target, buffer, cancellationToken).ConfigureAwait(false);

                //var position = fs.Position;
                //_logger.LogDebug("Streamed {0} bytes to position {1} from file {2}", bytesRead, position, path);

                if (bytesRead == 0)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            }
        }

        private static async Task<int> CopyToAsyncInternal(Stream source, Stream destination, byte[] buffer, CancellationToken cancellationToken)
        {
            int bytesRead;
            int totalBytesRead = 0;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
    }
}
