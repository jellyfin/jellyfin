#nullable disable

#pragma warning disable CA1711
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.TunerHosts
{
    public class LiveStream : ILiveStream
    {
        private readonly IConfigurationManager _configurationManager;
        private readonly object _chunkLock = new();
        private readonly List<string> _chunkPaths = new();
        private readonly bool _isRollingChunkEnabled;
        private readonly string _singleFilePath;
        private int _nextChunkIndex;
        private string _chunkDirectory = string.Empty;

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

            if (tuner is not null)
            {
                TunerHostId = tuner.Id;
            }

            _configurationManager = configurationManager;
            StreamHelper = streamHelper;

            ConsumerCount = 1;
            _isRollingChunkEnabled = configurationManager.GetEncodingOptions().LiveStreamKeepSeconds > 0;
            SetChunkDirectory();
            _singleFilePath = Path.Combine(configurationManager.GetTranscodePath(), UniqueId + ".ts");
        }

        protected IFileSystem FileSystem { get; }

        protected IStreamHelper StreamHelper { get; }

        protected ILogger Logger { get; }

        protected CancellationTokenSource LiveStreamCancellationTokenSource { get; } = new CancellationTokenSource();

        internal string ChunkDirectory => _chunkDirectory;

        protected bool IsRollingChunkEnabled => _isRollingChunkEnabled;

        public MediaSourceInfo OriginalMediaSource { get; set; }

        public MediaSourceInfo MediaSource { get; set; }

        public int ConsumerCount { get; set; }

        public string OriginalStreamId { get; set; }

        public bool EnableStreamSharing { get; set; }

        public string UniqueId { get; }

        public string TunerHostId { get; }

        public DateTime DateOpened { get; protected set; }

        private void SetChunkDirectory()
        {
            _chunkDirectory = Path.Combine(_configurationManager.GetTranscodePath(), UniqueId);
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
            lock (_chunkLock)
            {
                if (_chunkPaths.Count == 0)
                {
                    throw new InvalidOperationException("Live stream has not started yet.");
                }
            }

            if (!_isRollingChunkEnabled)
            {
                return new FileStream(
                    _singleFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete,
                    IODefaults.FileStreamBufferSize,
                    FileOptions.Asynchronous);
            }

            bool seekToLiveEdge = (DateTime.UtcNow - DateOpened).TotalSeconds > 10;
            return new RollingChunkStream(_chunkPaths, _chunkLock, Logger, seekToLiveEdge);
        }

        /// <summary>
        /// Opens the first chunk file for writing. Must be called before <see cref="RotateChunk"/>.
        /// </summary>
        /// <returns>A <see cref="FileStream"/> open for writing to the first chunk.</returns>
        protected FileStream OpenInitialChunk()
        {
            string path;
            if (_isRollingChunkEnabled)
            {
                Directory.CreateDirectory(_chunkDirectory);
                path = GetChunkPath(_nextChunkIndex++);
            }
            else
            {
                path = _singleFilePath;
            }

            lock (_chunkLock)
            {
                _chunkPaths.Add(path);
            }

            return OpenWriteStream(path);
        }

        /// <summary>
        /// Returns true when the current chunk has been open long enough to rotate.
        /// </summary>
        /// <param name="chunkOpenedAt">The UTC time when the current chunk was opened.</param>
        /// <returns><see langword="true"/> when the chunk should be rotated; otherwise <see langword="false"/>.</returns>
        protected bool ShouldRotateChunk(DateTime chunkOpenedAt)
        {
            var options = _configurationManager.GetEncodingOptions();
            if (options.LiveStreamKeepSeconds <= 0)
            {
                return false;
            }

            int chunkDurationSeconds = ComputeChunkDurationSeconds(options);
            return (DateTime.UtcNow - chunkOpenedAt).TotalSeconds >= chunkDurationSeconds;
        }

        /// <summary>
        /// Seals the current chunk, opens the next one, and deletes any chunk outside the keep window.
        /// Returns the new write stream and the time the new chunk was opened.
        /// </summary>
        /// <returns>The new <see cref="FileStream"/> and the UTC time it was opened.</returns>
        protected (FileStream NewStream, DateTime NewChunkTime) RotateChunk()
        {
            string newPath;
            string toDelete = null;

            lock (_chunkLock)
            {
                newPath = GetChunkPath(_nextChunkIndex++);
                _chunkPaths.Add(newPath);

                var options = _configurationManager.GetEncodingOptions();
                int maxChunks = ComputeMaxChunks(options);

                if (_chunkPaths.Count > maxChunks)
                {
                    toDelete = _chunkPaths[0];
                    _chunkPaths.RemoveAt(0);
                }
            }

            if (toDelete is not null)
            {
                try
                {
                    FileSystem.DeleteFile(toDelete);
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, "Error deleting live stream chunk {Path}", toDelete);
                }
            }

            return (OpenWriteStream(newPath), DateTime.UtcNow);
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

        protected async Task DeleteChunks(int retryCount = 0)
        {
            if (!_isRollingChunkEnabled)
            {
                if (retryCount == 0)
                {
                    Logger.LogInformation("Deleting live stream file {Path}", _singleFilePath);
                }

                try
                {
                    if (File.Exists(_singleFilePath))
                    {
                        File.Delete(_singleFilePath);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error deleting live stream file {Path}", _singleFilePath);
                    if (retryCount <= 40)
                    {
                        await Task.Delay(500).ConfigureAwait(false);
                        await DeleteChunks(retryCount + 1).ConfigureAwait(false);
                    }
                }

                return;
            }

            if (retryCount == 0)
            {
                Logger.LogInformation("Deleting live stream chunk directory {Directory}", _chunkDirectory);
            }

            try
            {
                if (Directory.Exists(_chunkDirectory))
                {
                    Directory.Delete(_chunkDirectory, recursive: true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error deleting chunk directory {Directory}", _chunkDirectory);
                if (retryCount <= 40)
                {
                    await Task.Delay(500).ConfigureAwait(false);
                    await DeleteChunks(retryCount + 1).ConfigureAwait(false);
                }
            }
        }

        private string GetChunkPath(int index)
            => Path.Combine(_chunkDirectory, $"chunk_{index.ToString(CultureInfo.InvariantCulture)}.ts");

        private static FileStream OpenWriteStream(string path)
            => new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write,
                FileShare.ReadWrite | FileShare.Delete,
                IODefaults.FileStreamBufferSize,
                FileOptions.Asynchronous);

        private static int ComputeChunkDurationSeconds(EncodingOptions options)
        {
            int keepSeconds = Math.Max(options.LiveStreamKeepSeconds, 60);
            return Math.Max(keepSeconds / 4, 30);
        }

        private static int ComputeMaxChunks(EncodingOptions options)
        {
            int keepSeconds = Math.Max(options.LiveStreamKeepSeconds, 60);
            int chunkDuration = Math.Max(keepSeconds / 4, 30);
            // +1 for the in-progress chunk being written
            return (keepSeconds / chunkDuration) + 1;
        }
    }
}
