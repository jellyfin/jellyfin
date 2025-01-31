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
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private bool _hasExited;
        private FileStream _logFileStream;
        private string _targetPath;
        private Process _process;
        private bool _disposed;

        public EncodedRecorder(
            ILogger logger,
            IMediaEncoder mediaEncoder,
            IServerApplicationPaths appPaths,
            IServerConfigurationManager serverConfigurationManager)
        {
            _logger = logger;
            _mediaEncoder = mediaEncoder;
            _appPaths = appPaths;
            _serverConfigurationManager = serverConfigurationManager;
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

            var processStartInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,

                RedirectStandardError = true,
                RedirectStandardInput = true,

                FileName = _mediaEncoder.EncoderPath,
                Arguments = GetCommandLineArgs(mediaSource, inputFile, targetFile),

                WindowStyle = ProcessWindowStyle.Hidden,
                ErrorDialog = false
            };

            _logger.LogInformation("{Filename} {Arguments}", processStartInfo.FileName, processStartInfo.Arguments);

            var logFilePath = Path.Combine(_appPaths.LogDirectoryPath, "record-transcode-" + Guid.NewGuid() + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            _logFileStream = new FileStream(logFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, FileOptions.Asynchronous);

            await JsonSerializer.SerializeAsync(_logFileStream, mediaSource, _jsonOptions, cancellationToken).ConfigureAwait(false);
            await _logFileStream.WriteAsync(Encoding.UTF8.GetBytes(Environment.NewLine + Environment.NewLine + processStartInfo.FileName + " " + processStartInfo.Arguments + Environment.NewLine + Environment.NewLine), cancellationToken).ConfigureAwait(false);

            _process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };
            _process.Exited += (_, _) => OnFfMpegProcessExited(_process);

            _process.Start();

            cancellationToken.Register(Stop);

            onStarted();

            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            _ = StartStreamingLog(_process.StandardError.BaseStream, _logFileStream);

            _logger.LogInformation("ffmpeg recording process started for {Path}", _targetPath);

            // Block until ffmpeg exits
            await _taskCompletionSource.Task.ConfigureAwait(false);
        }

        private string GetCommandLineArgs(MediaSourceInfo mediaSource, string inputTempFile, string targetFile)
        {
            string videoArgs = "-codec:v:0 copy -fflags +genpts";

            var flags = new List<string>();
            if (mediaSource.IgnoreDts)
            {
                flags.Add("+igndts");
            }

            if (mediaSource.IgnoreIndex)
            {
                flags.Add("+ignidx");
            }

            if (mediaSource.GenPtsInput)
            {
                flags.Add("+genpts");
            }

            var inputModifier = "-async 1";

            if (flags.Count > 0)
            {
                inputModifier += " -fflags " + string.Join(string.Empty, flags);
            }

            if (mediaSource.ReadAtNativeFramerate)
            {
                inputModifier += " -re";
            }

            if (mediaSource.RequiresLooping)
            {
                inputModifier += " -stream_loop -1 -reconnect_at_eof 1 -reconnect_streamed 1 -reconnect_delay_max 2";
            }

            var analyzeDurationSeconds = 5;
            var analyzeDuration = " -analyzeduration " +
                  (analyzeDurationSeconds * 1000000).ToString(CultureInfo.InvariantCulture);
            inputModifier += analyzeDuration;

            var subtitleArgs = CopySubtitles ? " -codec:s copy" : " -sn";

            // var outputParam = string.Equals(Path.GetExtension(targetFile), ".mp4", StringComparison.OrdinalIgnoreCase) ?
            //    " -f mp4 -movflags frag_keyframe+empty_moov" :
            //    string.Empty;

            var outputParam = string.Empty;

            var threads = EncodingHelper.GetNumberOfThreads(null, _serverConfigurationManager.GetEncodingOptions(), null);
            var commandLineArgs = string.Format(
                CultureInfo.InvariantCulture,
                "-i \"{0}\" {2} -map_metadata -1 -threads {6} {3}{4}{5} -y \"{1}\"",
                inputTempFile,
                targetFile.Replace("\"", "\\\"", StringComparison.Ordinal), // Escape quotes in filename
                videoArgs,
                GetAudioArgs(mediaSource),
                subtitleArgs,
                outputParam,
                threads);

            return inputModifier + " " + commandLineArgs;
        }

        private static string GetAudioArgs(MediaSourceInfo mediaSource)
        {
            return "-codec:a:0 copy";
        }

        protected string GetOutputSizeParam()
            => "-vf \"yadif=0:-1:0\"";

        private void Stop()
        {
            if (!_hasExited)
            {
                try
                {
                    _logger.LogInformation("Stopping ffmpeg recording process for {Path}", _targetPath);

                    _process.StandardInput.WriteLine("q");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping recording transcoding job for {Path}", _targetPath);
                }

                if (_hasExited)
                {
                    return;
                }

                try
                {
                    _logger.LogInformation("Calling recording process.WaitForExit for {Path}", _targetPath);

                    if (_process.WaitForExit(10000))
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error waiting for recording process to exit for {Path}", _targetPath);
                }

                if (_hasExited)
                {
                    return;
                }

                try
                {
                    _logger.LogInformation("Killing ffmpeg recording process for {Path}", _targetPath);

                    _process.Kill();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error killing recording transcoding job for {Path}", _targetPath);
                }
            }
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        private void OnFfMpegProcessExited(Process process)
        {
            using (process)
            {
                _hasExited = true;

                _logFileStream?.Dispose();
                _logFileStream = null;

                var exitCode = process.ExitCode;

                _logger.LogInformation("FFMpeg recording exited with code {ExitCode} for {Path}", exitCode, _targetPath);

                if (exitCode == 0)
                {
                    _taskCompletionSource.TrySetResult(true);
                }
                else
                {
                    _taskCompletionSource.TrySetException(
                        new FfmpegException(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Recording for {0} failed. Exit code {1}",
                                _targetPath,
                                exitCode)));
                }
            }
        }

        private async Task StartStreamingLog(Stream source, FileStream target)
        {
            try
            {
                using (var reader = new StreamReader(source))
                {
                    await foreach (var line in reader.ReadAllLinesAsync().ConfigureAwait(false))
                    {
                        var bytes = Encoding.UTF8.GetBytes(Environment.NewLine + line);

                        await target.WriteAsync(bytes.AsMemory()).ConfigureAwait(false);
                        await target.FlushAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading ffmpeg recording log");
            }
        }

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
                _process?.Dispose();
            }

            _logFileStream = null;
            _process = null;

            _disposed = true;
        }
    }
}
