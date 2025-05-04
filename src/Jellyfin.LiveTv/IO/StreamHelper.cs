#pragma warning disable CS1591

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;

namespace Jellyfin.LiveTv.IO
{
    public class StreamHelper : IStreamHelper
    {
        public async Task CopyToAsync(Stream source, Stream destination, int bufferSize, Action? onStarted, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                int read;
                while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);

                    if (onStarted is not null)
                    {
                        onStarted();
                        onStarted = null;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task CopyToAsync(Stream source, Stream destination, int bufferSize, int emptyReadLimit, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                if (emptyReadLimit <= 0)
                {
                    int read;
                    while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    }

                    return;
                }

                var eofCount = 0;

                while (eofCount < emptyReadLimit)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var bytesRead = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        eofCount++;
                        await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        eofCount = 0;

                        await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task CopyUntilCancelled(Stream source, Stream target, int bufferSize, CancellationToken cancellationToken)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var bytesRead = await CopyToAsyncInternal(source, target, buffer, cancellationToken).ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static async Task<int> CopyToAsyncInternal(Stream source, Stream destination, byte[] buffer, CancellationToken cancellationToken)
        {
            int bytesRead;
            int totalBytesRead = 0;

            while ((bytesRead = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);

                totalBytesRead += bytesRead;
            }

            return totalBytesRead;
        }
    }
}
