using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public class LiveStream : ILiveStream
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
        public string UniqueId { get; private set; }

        public List<string> SharedStreamIds { get; private set; }
        protected readonly IEnvironmentInfo Environment;
        protected readonly IFileSystem FileSystem;

        protected readonly string TempFilePath;
        protected readonly ILogger Logger;
        protected readonly CancellationTokenSource LiveStreamCancellationTokenSource = new CancellationTokenSource();

        public LiveStream(MediaSourceInfo mediaSource, IEnvironmentInfo environment, IFileSystem fileSystem, ILogger logger, IServerApplicationPaths appPaths)
        {
            OriginalMediaSource = mediaSource;
            Environment = environment;
            FileSystem = fileSystem;
            OpenedMediaSource = mediaSource;
            Logger = logger;
            EnableStreamSharing = true;
            SharedStreamIds = new List<string>();
            UniqueId = Guid.NewGuid().ToString("N");
            TempFilePath = Path.Combine(appPaths.TranscodingTempPath, UniqueId + ".ts");
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

        protected Stream GetInputStream(string path, bool allowAsyncFileRead)
        {
            var fileOpenOptions = FileOpenOptions.SequentialScan;

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
            catch (DirectoryNotFoundException)
            {
                return;
            }
            catch (FileNotFoundException)
            {
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

        public async Task CopyToAsync(Stream stream, CancellationToken cancellationToken)
        {
            cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, LiveStreamCancellationTokenSource.Token).Token;

            var allowAsync = false;//Environment.OperatingSystem != MediaBrowser.Model.System.OperatingSystem.Windows;
            // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039

            using (var inputStream = (FileStream)GetInputStream(TempFilePath, allowAsync))
            {
                TrySeek(inputStream, -20000);

                await CopyTo(inputStream, stream, 81920, null, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task CopyTo(Stream source, Stream destination, int bufferSize, Action onStarted, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[bufferSize];

            var eofCount = 0;
            var emptyReadLimit = 1000;

            while (eofCount < emptyReadLimit)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bytesRead = source.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    eofCount++;
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    eofCount = 0;

                    //await destination.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                    destination.Write(buffer, 0, bytesRead);

                    if (onStarted != null)
                    {
                        onStarted();
                        onStarted = null;
                    }
                }
            }
        }

        private void TrySeek(FileStream stream, long offset)
        {
            try
            {
                stream.Seek(offset, SeekOrigin.End);
            }
            catch (IOException)
            {

            }
            catch (ArgumentException)
            {

            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error seeking stream", ex);
            }
        }
    }
}
