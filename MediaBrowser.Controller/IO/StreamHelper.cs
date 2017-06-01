using System.IO;
using System.Threading;
using System;

namespace MediaBrowser.Controller.IO
{
    public static class StreamHelper
    {
        public static void CopyTo(Stream source, Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            CopyTo(source, destination, bufferSize, null, cancellationToken);
        }

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
    }
}
