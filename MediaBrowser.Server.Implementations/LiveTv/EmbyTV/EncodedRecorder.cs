using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public class EncodedRecorder : IRecorder
    {
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClient _httpClient;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IServerApplicationPaths _appPaths;
        private readonly LiveTvOptions _liveTvOptions;
        private bool _hasExited;
        private Stream _logFileStream;
        private string _targetPath;
        private Process _process;
        private readonly IJsonSerializer _json;
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

        public EncodedRecorder(ILogger logger, IFileSystem fileSystem, IMediaEncoder mediaEncoder, IServerApplicationPaths appPaths, IJsonSerializer json, LiveTvOptions liveTvOptions, IHttpClient httpClient)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _mediaEncoder = mediaEncoder;
            _appPaths = appPaths;
            _json = json;
            _liveTvOptions = liveTvOptions;
            _httpClient = httpClient;
        }

        public string GetOutputPath(MediaSourceInfo mediaSource, string targetFile)
        {
            return Path.ChangeExtension(targetFile, ".mp4");
        }

        public async Task Record(MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            var tempfile = Path.Combine(_appPaths.TranscodingTempPath, Guid.NewGuid().ToString("N") + ".ts");

            try
            {
                await RecordInternal(mediaSource, tempfile, targetFile, duration, onStarted, cancellationToken)
                        .ConfigureAwait(false);
            }
            finally
            {
                try
                {
                    File.Delete(tempfile);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error deleting recording temp file", ex);
                }
            }
        }

        public async Task RecordInternal(MediaSourceInfo mediaSource, string tempFile, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            var httpRequestOptions = new HttpRequestOptions()
            {
                Url = mediaSource.Path
            };

            httpRequestOptions.BufferContent = false;

            using (var response = await _httpClient.SendAsync(httpRequestOptions, "GET").ConfigureAwait(false))
            {
                _logger.Info("Opened recording stream from tuner provider");

                Directory.CreateDirectory(Path.GetDirectoryName(tempFile));

                using (var output = _fileSystem.GetFileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    //onStarted();

                    _logger.Info("Copying recording stream to file {0}", tempFile);

                    var bufferMs = 5000;

                    if (mediaSource.RunTimeTicks.HasValue)
                    {
                        // The media source already has a fixed duration
                        // But add another stop 1 minute later just in case the recording gets stuck for any reason
                        var durationToken = new CancellationTokenSource(duration.Add(TimeSpan.FromMinutes(1)));
                        cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token).Token;
                    }
                    else
                    {
                        // The media source if infinite so we need to handle stopping ourselves
                        var durationToken = new CancellationTokenSource(duration.Add(TimeSpan.FromMilliseconds(bufferMs)));
                        cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token).Token;
                    }

                    var tempFileTask = response.Content.CopyToAsync(output, StreamDefaults.DefaultCopyToBufferSize, cancellationToken);

                    // Give the temp file a little time to build up
                    await Task.Delay(bufferMs, cancellationToken).ConfigureAwait(false);

                    var recordTask = Task.Run(() => RecordFromFile(mediaSource, tempFile, targetFile, duration, onStarted, cancellationToken), cancellationToken);

                    await tempFileTask.ConfigureAwait(false);

                    await recordTask.ConfigureAwait(false);
                }
            }

            _logger.Info("Recording completed to file {0}", targetFile);
        }

        private Task RecordFromFile(MediaSourceInfo mediaSource, string inputFile, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            _targetPath = targetFile;
            _fileSystem.CreateDirectory(Path.GetDirectoryName(targetFile));

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    // Must consume both stdout and stderr or deadlocks may occur
                    //RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,

                    FileName = _mediaEncoder.EncoderPath,
                    Arguments = GetCommandLineArgs(mediaSource, inputFile, targetFile, duration),

                    WindowStyle = ProcessWindowStyle.Hidden,
                    ErrorDialog = false
                },

                EnableRaisingEvents = true
            };

            _process = process;

            var commandLineLogMessage = process.StartInfo.FileName + " " + process.StartInfo.Arguments;
            _logger.Info(commandLineLogMessage);

            var logFilePath = Path.Combine(_appPaths.LogDirectoryPath, "record-transcode-" + Guid.NewGuid() + ".txt");
            _fileSystem.CreateDirectory(Path.GetDirectoryName(logFilePath));

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            _logFileStream = _fileSystem.GetFileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, true);

            var commandLineLogMessageBytes = Encoding.UTF8.GetBytes(_json.SerializeToString(mediaSource) + Environment.NewLine + Environment.NewLine + commandLineLogMessage + Environment.NewLine + Environment.NewLine);
            _logFileStream.Write(commandLineLogMessageBytes, 0, commandLineLogMessageBytes.Length);

            process.Exited += (sender, args) => OnFfMpegProcessExited(process);

            process.Start();

            cancellationToken.Register(Stop);

            // MUST read both stdout and stderr asynchronously or a deadlock may occurr
            //process.BeginOutputReadLine();

            onStarted();

            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            StartStreamingLog(process.StandardError.BaseStream, _logFileStream);

            return _taskCompletionSource.Task;
        }

        private string GetCommandLineArgs(MediaSourceInfo mediaSource, string inputTempFile, string targetFile, TimeSpan duration)
        {
            string videoArgs;
            if (EncodeVideo(mediaSource))
            {
                var maxBitrate = 25000000;
                videoArgs = string.Format(
                        "-codec:v:0 libx264 -force_key_frames expr:gte(t,n_forced*5) {0} -pix_fmt yuv420p -preset superfast -crf 23 -b:v {1} -maxrate {1} -bufsize ({1}*2) -vsync -1 -profile:v high -level 41",
                        GetOutputSizeParam(),
                        maxBitrate.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                videoArgs = "-codec:v:0 copy";
            }

            var durationParam = " -t " + _mediaEncoder.GetTimeParameter(duration.Ticks);
            var commandLineArgs = "-fflags +genpts -async 1 -vsync -1 -re -i \"{0}\"{4} -sn {2} -map_metadata -1 -threads 0 {3} -y \"{1}\"";

            if (mediaSource.ReadAtNativeFramerate)
            {
                commandLineArgs = "-re " + commandLineArgs;
            }

            commandLineArgs = string.Format(commandLineArgs, inputTempFile, targetFile, videoArgs, GetAudioArgs(mediaSource), durationParam);

            return commandLineArgs;
        }

        private string GetAudioArgs(MediaSourceInfo mediaSource)
        {
            // do not copy aac because many players have difficulty with aac_latm
            var copyAudio = new[] { "mp3" };
            var mediaStreams = mediaSource.MediaStreams ?? new List<MediaStream>();
            var inputAudioCodec = mediaStreams.Where(i => i.Type == MediaStreamType.Audio).Select(i => i.Codec).FirstOrDefault() ?? string.Empty;

            if (copyAudio.Contains(inputAudioCodec, StringComparer.OrdinalIgnoreCase))
            {
                return "-codec:a:0 copy";
            }
            if (_liveTvOptions.EnableOriginalAudioWithEncodedRecordings && !string.Equals(inputAudioCodec, "aac", StringComparison.OrdinalIgnoreCase))
            {
                return "-codec:a:0 copy";
            }

            var audioChannels = 2;
            var audioStream = mediaStreams.FirstOrDefault(i => i.Type == MediaStreamType.Audio);
            if (audioStream != null)
            {
                audioChannels = audioStream.Channels ?? audioChannels;
            }
            return "-codec:a:0 aac -strict experimental -ab 320000";
        }

        private bool EncodeVideo(MediaSourceInfo mediaSource)
        {
            var mediaStreams = mediaSource.MediaStreams ?? new List<MediaStream>();
            return !mediaStreams.Any(i => i.Type == MediaStreamType.Video && string.Equals(i.Codec, "h264", StringComparison.OrdinalIgnoreCase) && !i.IsInterlaced);
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
                    _logger.Info("Killing ffmpeg recording process for {0}", _targetPath);

                    //process.Kill();
                    _process.StandardInput.WriteLine("q");
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error killing transcoding job for {0}", ex, _targetPath);
                }
            }
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="process">The process.</param>
        private void OnFfMpegProcessExited(Process process)
        {
            _hasExited = true;

            DisposeLogStream();

            try
            {
                var exitCode = process.ExitCode;

                _logger.Info("FFMpeg recording exited with code {0} for {1}", exitCode, _targetPath);

                if (exitCode == 0)
                {
                    _taskCompletionSource.TrySetResult(true);
                }
                else
                {
                    _taskCompletionSource.TrySetException(new Exception(string.Format("Recording for {0} failed. Exit code {1}", _targetPath, exitCode)));
                }
            }
            catch
            {
                _logger.Error("FFMpeg recording exited with an error for {0}.", _targetPath);
                _taskCompletionSource.TrySetException(new Exception(string.Format("Recording for {0} failed", _targetPath)));
            }
        }

        private void DisposeLogStream()
        {
            if (_logFileStream != null)
            {
                try
                {
                    _logFileStream.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error disposing recording log stream", ex);
                }

                _logFileStream = null;
            }
        }

        private async void StartStreamingLog(Stream source, Stream target)
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
                // Don't spam the log. This doesn't seem to throw in windows, but sometimes under linux
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error reading ffmpeg recording log", ex);
            }
        }
    }
}
