#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.TunerHosts
{
    public class LiveStream : ILiveStream
    {
        private readonly IConfigurationManager _configurationManager;

        protected readonly IFileSystem FileSystem;

        protected readonly IStreamHelper StreamHelper;

        protected string TempFilePath;
        protected readonly ILogger Logger;
        protected readonly CancellationTokenSource LiveStreamCancellationTokenSource = new CancellationTokenSource();

        public LiveStream(
            MediaSourceInfo mediaSource,
            TunerHostInfo tuner,
            IFileSystem fileSystem,
            ILogger logger,
            IConfigurationManager configurationManager,
            IStreamHelper streamHelper)
        {
            OriginalMediaSource = mediaSource;
            FileSystem = fileSystem;
            MediaSource = mediaSource;
            Logger = logger;
            EnableStreamSharing = true;
            UniqueId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            if (tuner != null)
            {
                TunerHostId = tuner.Id;
            }

            _configurationManager = configurationManager;
            StreamHelper = streamHelper;

            ConsumerCount = 1;
            SetTempFilePath("ts");
        }

        protected virtual int EmptyReadLimit => 1000;

        public MediaSourceInfo OriginalMediaSource { get; set; }
        public MediaSourceInfo MediaSource { get; set; }

        public int ConsumerCount { get; set; }

        public string OriginalStreamId { get; set; }
        public bool EnableStreamSharing { get; set; }
        public string UniqueId { get; }

        public string TunerHostId { get; }

        public DateTime DateOpened { get; protected set; }

        protected void SetTempFilePath(string extension)
        {
            TempFilePath = Path.Combine(_configurationManager.GetTranscodePath(), UniqueId + "." + extension);
        }

        public virtual Task Open(CancellationToken openCancellationToken)
        {
            DateOpened = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public Task Close()
        {
            EnableStreamSharing = false;

            Logger.LogInformation("Closing {Type}", GetType().Name);

            LiveStreamCancellationTokenSource.Cancel();

            return Task.CompletedTask;
        }

        protected FileStream GetInputStream(string path, bool allowAsyncFileRead)
            => new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                IODefaults.FileStreamBufferSize,
                allowAsyncFileRead ? FileOptions.SequentialScan | FileOptions.Asynchronous : FileOptions.SequentialScan);

        public Task DeleteTempFiles()
        {
            return DeleteTempFiles(GetStreamFilePaths());
        }

        protected async Task DeleteTempFiles(IEnumerable<string> paths, int retryCount = 0)
        {
            if (retryCount == 0)
            {
                Logger.LogInformation("Deleting temp files {0}", paths);
            }

            var failedFiles = new List<string>();

            foreach (var path in paths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    FileSystem.DeleteFile(path);
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

            // use non-async filestream on windows along with read due to https://github.com/dotnet/corefx/issues/6039
            var allowAsync = Environment.OSVersion.Platform != PlatformID.Win32NT;

            bool seekFile = (DateTime.UtcNow - DateOpened).TotalSeconds > 10;

            var nextFileInfo = GetNextFile(null);
            var nextFile = nextFileInfo.file;
            var isLastFile = nextFileInfo.isLastFile;

            while (!string.IsNullOrEmpty(nextFile))
            {
                var emptyReadLimit = isLastFile ? EmptyReadLimit : 1;

                await CopyFile(nextFile, seekFile, emptyReadLimit, allowAsync, stream, cancellationToken).ConfigureAwait(false);

                seekFile = false;
                nextFileInfo = GetNextFile(nextFile);
                nextFile = nextFileInfo.file;
                isLastFile = nextFileInfo.isLastFile;
            }

            Logger.LogInformation("Live Stream ended.");
        }

        private (string file, bool isLastFile) GetNextFile(string currentFile)
        {
            var files = GetStreamFilePaths();

            if (string.IsNullOrEmpty(currentFile))
            {
                return (files.Last(), true);
            }

            var nextIndex = files.FindIndex(i => string.Equals(i, currentFile, StringComparison.OrdinalIgnoreCase)) + 1;

            var isLastFile = nextIndex == files.Count - 1;

            return (files.ElementAtOrDefault(nextIndex), isLastFile);
        }

        private async Task CopyFile(string path, bool seekFile, int emptyReadLimit, bool allowAsync, Stream stream, CancellationToken cancellationToken)
        {
            using (var inputStream = GetInputStream(path, allowAsync))
            {
                if (seekFile)
                {
                    TrySeek(inputStream, -20000);
                }

                await StreamHelper.CopyToAsync(
                    inputStream,
                    stream,
                    IODefaults.CopyToBufferSize,
                    emptyReadLimit,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private void TrySeek(FileStream stream, long offset)
        {
            if (!stream.CanSeek)
            {
                return;
            }

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
