using System.IO;
using System.Threading;
using System;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.IO
{
    public static class StreamHelper
    {
        public static void CopyTo(Stream source, Stream destination, int bufferSize, Action onStarted, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                destination.Write(buffer, 0, read);

                if (onStarted != null)
                {
                    onStarted();
                    onStarted = null;
                }
            }
        }

        public static async Task CopyToAsync(Stream source, Stream destination, int bufferSize, IProgress<double> progress, long contentLength, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[bufferSize];
            int read;
            long totalRead = 0;

            while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                destination.Write(buffer, 0, read);

                totalRead += read;

                double pct = totalRead;
                pct /= contentLength;
                pct *= 100;

                progress.Report(pct);
            }
        }
    }
}
