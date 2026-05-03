#nullable disable

#pragma warning disable CA1711
#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jellyfin.LiveTv.TunerHosts
{
    public class LiveStream : ILiveStream
    {
        private readonly IOptions<EncodingOptions> _encodingOptions;
        private readonly IServerApplicationPaths _appPaths;

        public LiveStream(
            MediaSourceInfo mediaSource,
            TunerHostInfo tuner,
            IFileSystem fileSystem,
            ILogger logger,
            IOptions<EncodingOptions> encodingOptions,
            IServerApplicationPaths appPaths,
            IStreamHelper streamHelper)
        {
            OriginalMediaSource = mediaSource;
            FileSystem = fileSystem;
            MediaSource = mediaSource;
            Logger = logger;
            _encodingOptions = encodingOptions;
            _appPaths = appPaths;
            EnableStreamSharing = true;
            UniqueId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            if (tuner is not null)
            {
                TunerHostId = tuner.Id;
            }

            StreamHelper = streamHelper;

            ConsumerCount = 1;
            SetTempFilePath("ts");
        }

        protected IFileSystem FileSystem { get; }

        protected IStreamHelper StreamHelper { get; }

        protected ILogger Logger { get; }

        protected CancellationTokenSource LiveStreamCancellationTokenSource { get; } = new CancellationTokenSource();

        protected string TempFilePath { get; set; }

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
            TempFilePath = Path.Combine(EncodingConfigurationExtensions.GetTranscodePath(_encodingOptions.Value, _appPaths), UniqueId + "." + extension);
        }

        public virtual Task Open(CancellationToken openCancellationToken)
        {
            DateOpened = DateTime.UtcNow;
            return Task.CompletedTask;
        }

        public async Task Close()
        {
            EnableStreamSharing = false;

            Logger.LogInformation("Closing {Type}", GetType().Name);

            await LiveStreamCancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }

        public Stream GetStream()
        {
            var stream = new FileStream(
                TempFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                IODefaults.FileStreamBufferSize,
                FileOptions.SequentialScan | FileOptions.Asynchronous);

            bool seekFile = (DateTime.UtcNow - DateOpened).TotalSeconds > 10;
            if (seekFile)
            {
                TrySeek(stream, -20000);
            }

            return stream;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                LiveStreamCancellationTokenSource?.Dispose();
            }
        }

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
