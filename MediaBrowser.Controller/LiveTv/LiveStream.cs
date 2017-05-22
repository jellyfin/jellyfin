using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.System;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveStream
    {
        public MediaSourceInfo OriginalMediaSource { get; set; }
        public MediaSourceInfo OpenedMediaSource { get; set; }
        public int ConsumerCount
        {
            get { return SharedStreamIds.Count; }
        }
        public ITunerHost TunerHost { get; set; }
        public string OriginalStreamId { get; set; }
        public bool EnableStreamSharing { get; set; }
        public string UniqueId = Guid.NewGuid().ToString("N");

        public List<string> SharedStreamIds = new List<string>();
        protected readonly IEnvironmentInfo Environment;
        protected readonly IFileSystem FileSystem;
        const int StreamCopyToBufferSize = 81920;

        public LiveStream(MediaSourceInfo mediaSource, IEnvironmentInfo environment, IFileSystem fileSystem)
        {
            OriginalMediaSource = mediaSource;
            Environment = environment;
            FileSystem = fileSystem;
            OpenedMediaSource = mediaSource;
            EnableStreamSharing = true;
        }

        public Task Open(CancellationToken cancellationToken)
        {
            return OpenInternal(cancellationToken);
        }

        protected virtual Task OpenInternal(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public virtual Task Close()
        {
            return Task.FromResult(true);
        }

        private Stream GetInputStream(string path, long startPosition, bool allowAsyncFileRead)
        {
            var fileOpenOptions = startPosition > 0
                ? FileOpenOptions.RandomAccess
                : FileOpenOptions.SequentialScan;

            if (allowAsyncFileRead)
            {
                fileOpenOptions |= FileOpenOptions.Asynchronous;
            }

            return FileSystem.GetFileStream(path, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.ReadWrite, fileOpenOptions);
        }

        protected async Task DeleteTempFile(string path, int retryCount = 0)
        {
            try
            {
                FileSystem.DeleteFile(path);
                return;
            }
            catch
            {

            }

            if (retryCount > 20)
            {
                return;
            }

            await Task.Delay(500).ConfigureAwait(false);
            await DeleteTempFile(path, retryCount + 1).ConfigureAwait(false);
        }

        protected async Task CopyFileTo(string path, bool allowEndOfFile, Stream outputStream, CancellationToken cancellationToken)
        {
            var eofCount = 0;

            long startPosition = -25000;
            if (startPosition < 0)
            {
                var length = FileSystem.GetFileInfo(path).Length;
                startPosition = Math.Max(length - startPosition, 0);
            }

            // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039
            var allowAsyncFileRead = Environment.OperatingSystem != OperatingSystem.Windows;

            using (var inputStream = GetInputStream(path, startPosition, allowAsyncFileRead))
            {
                if (startPosition > 0)
                {
                    inputStream.Position = startPosition;
                }

                while (eofCount < 20 || !allowEndOfFile)
                {
                    int bytesRead;
                    if (allowAsyncFileRead)
                    {
                        bytesRead = await CopyToInternalAsync(inputStream, outputStream, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        bytesRead = await CopyToInternalAsyncWithSyncRead(inputStream, outputStream, cancellationToken).ConfigureAwait(false);
                    }

                    //var position = fs.Position;
                    //_logger.Debug("Streamed {0} bytes to position {1} from file {2}", bytesRead, position, path);

                    if (bytesRead == 0)
                    {
                        eofCount++;
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        eofCount = 0;
                    }
                }
            }
        }

        private async Task<int> CopyToInternalAsyncWithSyncRead(Stream source, Stream destination, CancellationToken cancellationToken)
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

        private async Task<int> CopyToInternalAsync(Stream source, Stream destination, CancellationToken cancellationToken)
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
    }
}
