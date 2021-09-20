#nullable disable

#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
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

        public Stream GetStream()
        {
            var stream = GetInputStream(TempFilePath, AsyncFile.UseAsyncIO);
            bool seekFile = (DateTime.UtcNow - DateOpened).TotalSeconds > 10;
            if (seekFile)
            {
                TrySeek(stream, -20000);
            }

            return stream;
        }

        protected FileStream GetInputStream(string path, bool allowAsyncFileRead)
            => new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                IODefaults.FileStreamBufferSize,
                allowAsyncFileRead ? FileOptions.SequentialScan | FileOptions.Asynchronous : FileOptions.SequentialScan);

        protected async Task DeleteTempFiles(string path, int retryCount = 0)
        {
            if (retryCount == 0)
            {
                Logger.LogInformation("Deleting temp file {FilePath}", path);
            }

            try
            {
                FileSystem.DeleteFile(path);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting file {FilePath}", path);
                if (retryCount <= 40)
                {
                    await Task.Delay(500).ConfigureAwait(false);
                    await DeleteTempFiles(path, retryCount + 1).ConfigureAwait(false);
                }
            }
        }

        private void TrySeek(Stream stream, long offset)
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
