#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
{
    public class EncodedRecorder : IRecorder
    {
        private readonly ILogger _logger;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IServerApplicationPaths _appPaths;
        private bool _hasExited;
        private Stream _logFileStream;
        private string _targetPath;
        private Process _process;
        private readonly IJsonSerializer _json;
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
        private readonly IServerConfigurationManager _config;

        public EncodedRecorder(
            ILogger logger,
            IMediaEncoder mediaEncoder,
            IServerApplicationPaths appPaths,
            IJsonSerializer json,
            IServerConfigurationManager config)
        {
            _logger = logger;
            _mediaEncoder = mediaEncoder;
            _appPaths = appPaths;
            _json = json;
            _config = config;
        }

        private static bool CopySubtitles => false;

        public string GetOutputPath(MediaSourceInfo mediaSource, string targetFile)
        {
            return Path.ChangeExtension(targetFile, ".ts");
        }

        public async Task Record(IDirectStreamProvider directStreamProvider, MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            // The media source is infinite so we need to handle stopping ourselves
            var durationToken = new CancellationTokenSource(duration);
            cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token).Token;

            await RecordFromFile(mediaSource, mediaSource.Path, targetFile, duration, onStarted, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Recording completed to file {0}", targetFile);
        }

        private EncodingOptions GetEncodingOptions()
        {
            return _config.GetConfiguration<EncodingOptions>("encoding");
        }

        private Task RecordFromFile(MediaSourceInfo mediaSource, string inputFile, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
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
                Arguments = GetCommandLineArgs(mediaSource, inputFile, targetFile, duration),

                WindowStyle = ProcessWindowStyle.Hidden,
                ErrorDialog = false
            };

            var commandLineLogMessage = processStartInfo.FileName + " " + processStartInfo.Arguments;
            _logger.LogInformation(commandLineLogMessage);

            var logFilePath = Path.Combine(_appPaths.LogDirectoryPath, "record-transcode-" + Guid.NewGuid() + ".txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            _logFileStream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, true);

            var commandLineLogMessageBytes = Encoding.UTF8.GetBytes(_json.SerializeToString(mediaSource) + Environment.NewLine + Environment.NewLine + commandLineLogMessage + Environment.NewLine + Environment.NewLine);
            _logFileStream.Write(commandLineLogMessageBytes, 0, commandLineLogMessageBytes.Length);

            _process = new Process
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true
            };
            _process.Exited += (sender, args) => OnFfMpegProcessExited(_process, inputFile);

            _process.Start();

            cancellationToken.Register(Stop);

            onStarted();

            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            _ = StartStreamingLog(_process.StandardError.BaseStream, _logFileStream);

            _logger.LogInformation("ffmpeg recording process started for {0}", _targetPath);

            return _taskCompletionSource.Task;
        }

        private string GetCommandLineArgs(MediaSourceInfo mediaSource, string inputTempFile, string targetFile, TimeSpan duration)
        {
            string videoArgs;
            if (EncodeVideo(mediaSource))
            {
                const int MaxBitrate = 25000000;
                videoArgs = string.Format(
                    CultureInfo.InvariantCulture,
                    "-codec:v:0 libx264 -force_key_frames \"expr:gte(t,n_forced*5)\" {0} -pix_fmt yuv420p -preset superfast -crf 23 -b:v {1} -maxrate {1} -bufsize ({1}*2) -vsync -1 -profile:v high -level 41",
                    GetOutputSizeParam(),
                    MaxBitrate);
            }
            else
            {
                videoArgs = "-codec:v:0 copy";
            }

            videoArgs += " -fflags +genpts";

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

            var inputModifier = "-async 1 -vsync -1";

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

            //var outputParam = string.Equals(Path.GetExtension(targetFile), ".mp4", StringComparison.OrdinalIgnoreCase) ?
            //    " -f mp4 -movflags frag_keyframe+empty_moov" :
            //    string.Empty;

            var outputParam = string.Empty;

            var commandLineArgs = string.Format(
                CultureInfo.InvariantCulture,
                "-i \"{0}\" {2} -map_metadata -1 -threads 0 {3}{4}{5} -y \"{1}\"",
                inputTempFile,
                targetFile,
                videoArgs,
                GetAudioArgs(mediaSource),
                subtitleArgs,
                outputParam);

            return inputModifier + " " + commandLineArgs;
        }

        private static string GetAudioArgs(MediaSourceInfo mediaSource)
        {
            return "-codec:a:0 copy";

            //var audioChannels = 2;
            //var audioStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);
            //if (audioStream != null)
            //{
            //    audioChannels = audioStream.Channels ?? audioChannels;
            //}
            //return "-codec:a:0 aac -strict experimental -ab 320000";
        }

        private static bool EncodeVideo(MediaSourceInfo mediaSource)
        {
            return false;
        }

        protected string GetOutputSizeParam()
        {
            var filters = new List<string>();

            filters.Add("yadif=0:-1:0");

            var output = string.Empty;

            if (filters.Count > 0)
            {
                output += string.Format(" -vf \"{0}\"", string.Join(",", filters.ToArray()));
            }

            return output;
        }

        private void Stop()
        {
            if (!_hasExited)
            {
                try
                {
                    _logger.LogInformation("Stopping ffmpeg recording process for {path}", _targetPath);

                    _process.StandardInput.WriteLine("q");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping recording transcoding job for {path}", _targetPath);
                }

                if (_hasExited)
                {
                    return;
                }

                try
                {
                    _logger.LogInformation("Calling recording process.WaitForExit for {path}", _targetPath);

                    if (_process.WaitForExit(10000))
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error waiting for recording process to exit for {path}", _targetPath);
                }

                if (_hasExited)
                {
                    return;
                }

                try
                {
                    _logger.LogInformation("Killing ffmpeg recording process for {path}", _targetPath);

                    _process.Kill();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error killing recording transcoding job for {path}", _targetPath);
                }
            }
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        private void OnFfMpegProcessExited(Process process, string inputFile)
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
                        new Exception(
                            string.Format(
                                CultureInfo.InvariantCulture,
                                "Recording for {0} failed. Exit code {1}",
                                _targetPath,
                                exitCode)));
                }
            }
        }

        private async Task StartStreamingLog(Stream source, Stream target)
        {
            try
            {
                using (var reader = new StreamReader(source))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);

                        var bytes = Encoding.UTF8.GetBytes(Environment.NewLine + line);

                        await target.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                        await target.FlushAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // TODO Investigate and properly fix.
                // Don't spam the log. This doesn't seem to throw in windows, but sometimes under linux
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading ffmpeg recording log");
            }
        }
    }
}
