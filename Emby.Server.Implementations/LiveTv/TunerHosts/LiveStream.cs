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
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.System;
using MediaBrowser.Model.LiveTv;
using System.Linq;
using MediaBrowser.Controller.Library;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public class LiveStream : ILiveStream
    {
        public MediaSourceInfo OriginalMediaSource { get; set; }
        public MediaSourceInfo MediaSource { get; set; }

        public int ConsumerCount { get; set; }

        public string OriginalStreamId { get; set; }
        public bool EnableStreamSharing { get; set; }
        public string UniqueId { get; private set; }

        protected readonly IFileSystem FileSystem;
        protected readonly IServerApplicationPaths AppPaths;

        protected string TempFilePath;
        protected readonly ILogger Logger;
        protected readonly CancellationTokenSource LiveStreamCancellationTokenSource = new CancellationTokenSource();

        public string TunerHostId { get; private set; }

        public DateTime DateOpened { get; protected set; }

        public Func<Task> OnClose { get; set; }

        public LiveStream(MediaSourceInfo mediaSource, TunerHostInfo tuner, IFileSystem fileSystem, ILogger logger, IServerApplicationPaths appPaths)
        {
            OriginalMediaSource = mediaSource;
            FileSystem = fileSystem;
            MediaSource = mediaSource;
            Logger = logger;
            EnableStreamSharing = true;
            UniqueId = Guid.NewGuid().ToString("N");

            if (tuner != null)
            {
                TunerHostId = tuner.Id;
            }

            AppPaths = appPaths;

            ConsumerCount = 1;
            SetTempFilePath("ts");
        }

        protected void SetTempFilePath(string extension)
        {
            TempFilePath = Path.Combine(AppPaths.GetTranscodingTempPath(), UniqueId + "." + extension);
        }

        public virtual Task Open(CancellationToken openCancellationToken)
        {
            DateOpened = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task Close()
        {
            EnableStreamSharing = false;

            Logger.LogInformation("Closing " + GetType().Name);

            LiveStreamCancellationTokenSource.Cancel();

            if (OnClose != null)
            {
                return CloseWithExternalFn();
            }

            return Task.CompletedTask;
        }

        private async Task CloseWithExternalFn()
        {
            try
            {
                await OnClose().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error closing live stream");
            }
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

        public Task DeleteTempFiles()
        {
            return DeleteTempFiles(GetStreamFilePaths());
        }

        protected async Task DeleteTempFiles(List<string> paths, int retryCount = 0)
        {
            if (retryCount == 0)
            {
                Logger.LogInformation("Deleting temp files {0}", string.Join(", ", paths.ToArray()));
            }

            var failedFiles = new List<string>();

            foreach (var path in paths)
            {
                try
                {
                    FileSystem.DeleteFile(path);
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (FileNotFoundException)
                {
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error deleting file {path}", path);
                    failedFiles.Add(path);
                }
            }

            if (failedFiles.Count > 0 && retryCount <= 40)
            {
                await Task.Delay(500).ConfigureAwait(false);
                await DeleteTempFiles(failedFiles, retryCount + 1).ConfigureAwait(false);
            }
        }

        protected virtual List<string> GetStreamFilePaths()
        {
            return new List<string> { TempFilePath };
        }

        public async Task CopyToAsync(Stream stream, CancellationToken cancellationToken)
        {
            cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, LiveStreamCancellationTokenSource.Token).Token;

            var allowAsync = false;
            // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039

            bool seekFile = (DateTime.UtcNow - DateOpened).TotalSeconds > 10;

            var nextFileInfo = GetNextFile(null);
            var nextFile = nextFileInfo.Item1;
            var isLastFile = nextFileInfo.Item2;

            while (!string.IsNullOrEmpty(nextFile))
            {
                var emptyReadLimit = isLastFile ? EmptyReadLimit : 1;

                await CopyFile(nextFile, seekFile, emptyReadLimit, allowAsync, stream, cancellationToken).ConfigureAwait(false);

                seekFile = false;
                nextFileInfo = GetNextFile(nextFile);
                nextFile = nextFileInfo.Item1;
                isLastFile = nextFileInfo.Item2;
            }

            Logger.LogInformation("Live Stream ended.");
        }

        private Tuple<string, bool> GetNextFile(string currentFile)
        {
            var files = GetStreamFilePaths();

            //logger.LogInformation("Live stream files: {0}", string.Join(", ", files.ToArray()));

            if (string.IsNullOrEmpty(currentFile))
            {
                return new Tuple<string, bool>(files.Last(), true);
            }

            var nextIndex = files.FindIndex(i => string.Equals(i, currentFile, StringComparison.OrdinalIgnoreCase)) + 1;

            var isLastFile = nextIndex == files.Count - 1;

            return new Tuple<string, bool>(files.ElementAtOrDefault(nextIndex), isLastFile);
        }

        private async Task CopyFile(string path, bool seekFile, int emptyReadLimit, bool allowAsync, Stream stream, CancellationToken cancellationToken)
        {
            //logger.LogInformation("Opening live stream file {0}. Empty read limit: {1}", path, emptyReadLimit);

            using (var inputStream = (FileStream)GetInputStream(path, allowAsync))
            {
                if (seekFile)
                {
                    TrySeek(inputStream, -20000);
                }

                await ApplicationHost.StreamHelper.CopyToAsync(inputStream, stream, 81920, emptyReadLimit, cancellationToken).ConfigureAwait(false);
            }
        }

        protected virtual int EmptyReadLimit
        {
            get
            {
                return 1000;
            }
        }

        private void TrySeek(FileStream stream, long offset)
        {
            //logger.LogInformation("TrySeek live stream");
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
                Logger.LogError(ex, "Error seeking stream");
            }
        }
    }
}
