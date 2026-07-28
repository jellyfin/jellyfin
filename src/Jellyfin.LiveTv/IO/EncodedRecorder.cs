#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.MediaEncoding.FFProcessing;
using MediaBrowser.Controller.MediaEncoding.FFProcessing.Requests;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv.IO
{
    public class EncodedRecorder : IRecorder
    {
        private readonly ILogger _logger;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IServerApplicationPaths _appPaths;
        private readonly IFFRunner _ffRunner;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private FileStream _logFileStream;
        private string _targetPath;
        private bool _disposed;

        public EncodedRecorder(
            ILogger logger,
            IMediaEncoder mediaEncoder,
            IServerApplicationPaths appPaths,
            IFFRunner ffRunner)
        {
            _logger = logger;
            _mediaEncoder = mediaEncoder;
            _appPaths = appPaths;
            _ffRunner = ffRunner;
        }

        private static bool CopySubtitles => false;

        public string GetOutputPath(MediaSourceInfo mediaSource, string targetFile)
        {
            return Path.ChangeExtension(targetFile, ".ts");
        }

        public async Task Record(IDirectStreamProvider directStreamProvider, MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            // The media source is infinite so we need to handle stopping ourselves
            using var durationToken = new CancellationTokenSource(duration);
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token);

            await RecordFromFile(mediaSource, mediaSource.Path, targetFile, onStarted, cancellationTokenSource.Token).ConfigureAwait(false);

            _logger.LogInformation("Recording completed to file {Path}", targetFile);
        }

        private async Task RecordFromFile(MediaSourceInfo mediaSource, string inputFile, string targetFile, Action onStarted, CancellationToken cancellationToken)
        {
            _targetPath = targetFile;
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
            if (!File.Exists(targetFile))
            {
                FileHelper.CreateEmpty(targetFile);
            }

            var logFilePath = Path.Combine(_appPaths.LogDirectoryPath, "record-transcode-" + Guid.NewGuid() + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            _logFileStream = new FileStream(logFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);

            var request = BuildRequest(mediaSource, inputFile, targetFile);

            await JsonSerializer.SerializeAsync(_logFileStream, mediaSource, _jsonOptions, cancellationToken).ConfigureAwait(false);
            await _logFileStream.WriteAsync(
                Encoding.UTF8.GetBytes(Environment.NewLine + Environment.NewLine + _mediaEncoder.EncoderPath + Environment.NewLine + Environment.NewLine),
                cancellationToken).ConfigureAwait(false);

            // Cancellation is the stop signal: the runner writes q, waits the action's grace period,
            // then kills the tree. That replaces the old Register(Stop) plus manual escalation.
            IFFSession session;
            try
            {
                session = await _ffRunner.StartAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Nothing will complete, so the path that normally closes this never runs.
                await _logFileStream.DisposeAsync().ConfigureAwait(false);
                _logFileStream = null;

                throw;
            }

            _logger.LogInformation("ffmpeg recording process started for {Path}", _targetPath);
            onStarted();

            var result = await session.Completion.ConfigureAwait(false);

            if (_logFileStream is not null)
            {
                await _logFileStream.DisposeAsync().ConfigureAwait(false);
                _logFileStream = null;
            }

            if (result.StopReason == FFStopReason.Cancelled)
            {
                // The recording was stopped deliberately, by the duration cap or by the user.
                return;
            }

            if (!result.Succeeded)
            {
                throw new FfmpegException($"Recording for {_targetPath} failed. Exit code {result.ExitCode}");
            }
        }

        private RecordRequest BuildRequest(MediaSourceInfo mediaSource, string inputTempFile, string targetFile)
        {
            return new RecordRequest
            {
                Input = "\"" + inputTempFile + "\"",
                OutputPath = "\"" + targetFile.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"",
                EncoderVersion = _mediaEncoder.EncoderVersion,
                IgnoreDts = mediaSource.IgnoreDts,
                IgnoreIndex = mediaSource.IgnoreIndex,
                GeneratePts = mediaSource.GenPtsInput,
                ReadAtNativeFramerate = mediaSource.ReadAtNativeFramerate,
                RequiresLooping = mediaSource.RequiresLooping,
                CopySubtitles = CopySubtitles,

                // Written through as it arrives so the log is readable while the recording runs.
                Stderr = FFOutputSink.ToStream(_logFileStream)
            };
        }

        private static string GetAudioArgs(MediaSourceInfo mediaSource)
        {
            return "-codec:a:0 copy";
        }

        protected string GetOutputSizeParam()
            => "-vf \"yadif=0:-1:0\"";

        /// <summary>
        /// Processes the exited.
        /// </summary>

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _logFileStream?.Dispose();
            }

            _logFileStream = null;

            _disposed = true;
        }
    }
}
