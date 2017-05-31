using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;

using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Diagnostics;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
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
        private IProcess _process;
        private readonly IProcessFactory _processFactory;
        private readonly IJsonSerializer _json;
        private readonly TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();
        private readonly IServerConfigurationManager _config;

        public EncodedRecorder(ILogger logger, IFileSystem fileSystem, IMediaEncoder mediaEncoder, IServerApplicationPaths appPaths, IJsonSerializer json, LiveTvOptions liveTvOptions, IHttpClient httpClient, IProcessFactory processFactory, IServerConfigurationManager config)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _mediaEncoder = mediaEncoder;
            _appPaths = appPaths;
            _json = json;
            _liveTvOptions = liveTvOptions;
            _httpClient = httpClient;
            _processFactory = processFactory;
            _config = config;
        }

        private string OutputFormat
        {
            get
            {
                var format = _liveTvOptions.RecordingEncodingFormat;

                if (string.Equals(format, "mkv", StringComparison.OrdinalIgnoreCase))
                {
                    return "mkv";
                }
                if (string.Equals(format, "ts", StringComparison.OrdinalIgnoreCase))
                {
                    return "ts";
                }

                return "mp4";
            }
        }

        private bool CopySubtitles
        {
            get
            {
                return false;
                //return string.Equals(OutputFormat, "mkv", StringComparison.OrdinalIgnoreCase);
            }
        }

        public string GetOutputPath(MediaSourceInfo mediaSource, string targetFile)
        {
            var extension = OutputFormat;

            if (string.Equals(extension, "mpegts", StringComparison.OrdinalIgnoreCase))
            {
                extension = "ts";
            }

            return Path.ChangeExtension(targetFile, "." + extension);
        }

        public async Task Record(IDirectStreamProvider directStreamProvider, MediaSourceInfo mediaSource, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            //var durationToken = new CancellationTokenSource(duration);
            //cancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken.Token).Token;

            await RecordFromFile(mediaSource, mediaSource.Path, targetFile, duration, onStarted, cancellationToken).ConfigureAwait(false);

            _logger.Info("Recording completed to file {0}", targetFile);
        }

        private EncodingOptions GetEncodingOptions()
        {
            return _config.GetConfiguration<EncodingOptions>("encoding");
        }

        private Task RecordFromFile(MediaSourceInfo mediaSource, string inputFile, string targetFile, TimeSpan duration, Action onStarted, CancellationToken cancellationToken)
        {
            _targetPath = targetFile;
            _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(targetFile));

            var process = _processFactory.Create(new ProcessOptions
            {
                CreateNoWindow = true,
                UseShellExecute = false,

                // Must consume both stdout and stderr or deadlocks may occur
                //RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,

                FileName = _mediaEncoder.EncoderPath,
                Arguments = GetCommandLineArgs(mediaSource, inputFile, targetFile, duration),

                IsHidden = true,
                ErrorDialog = false,
                EnableRaisingEvents = true
            });

            _process = process;

            var commandLineLogMessage = process.StartInfo.FileName + " " + process.StartInfo.Arguments;
            _logger.Info(commandLineLogMessage);

            var logFilePath = Path.Combine(_appPaths.LogDirectoryPath, "record-transcode-" + Guid.NewGuid() + ".txt");
            _fileSystem.CreateDirectory(_fileSystem.GetDirectoryName(logFilePath));

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            _logFileStream = _fileSystem.GetFileStream(logFilePath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, true);

            var commandLineLogMessageBytes = Encoding.UTF8.GetBytes(_json.SerializeToString(mediaSource) + Environment.NewLine + Environment.NewLine + commandLineLogMessage + Environment.NewLine + Environment.NewLine);
            _logFileStream.Write(commandLineLogMessageBytes, 0, commandLineLogMessageBytes.Length);

            process.Exited += (sender, args) => OnFfMpegProcessExited(process, inputFile);

            process.Start();

            cancellationToken.Register(Stop);

            // MUST read both stdout and stderr asynchronously or a deadlock may occurr
            //process.BeginOutputReadLine();

            onStarted();

            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            StartStreamingLog(process.StandardError.BaseStream, _logFileStream);

            _logger.Info("ffmpeg recording process started for {0}", _targetPath);

            return _taskCompletionSource.Task;
        }

        private string GetCommandLineArgs(MediaSourceInfo mediaSource, string inputTempFile, string targetFile, TimeSpan duration)
        {
            string videoArgs;
            if (EncodeVideo(mediaSource))
            {
                var maxBitrate = 25000000;
                videoArgs = string.Format(
                        "-codec:v:0 libx264 -force_key_frames \"expr:gte(t,n_forced*5)\" {0} -pix_fmt yuv420p -preset superfast -crf 23 -b:v {1} -maxrate {1} -bufsize ({1}*2) -vsync -1 -profile:v high -level 41",
                        GetOutputSizeParam(),
                        maxBitrate.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                videoArgs = "-codec:v:0 copy";
            }

            videoArgs += " -fflags +genpts";

            var durationParam = " -t " + _mediaEncoder.GetTimeParameter(duration.Ticks);

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
                inputModifier += " -fflags " + string.Join("", flags.ToArray());
            }

            if (!string.IsNullOrWhiteSpace(GetEncodingOptions().HardwareAccelerationType))
            {
                inputModifier += " -hwaccel auto";
            }

            if (mediaSource.ReadAtNativeFramerate)
            {
                inputModifier += " -re";
            }

            if (mediaSource.RequiresLooping)
            {
                inputModifier += " -stream_loop -1";
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

            var commandLineArgs = string.Format("-i \"{0}\"{5} {2} -map_metadata -1 -threads 0 {3}{4}{6} -y \"{1}\"", 
                inputTempFile, 
                targetFile, 
                videoArgs, 
                GetAudioArgs(mediaSource), 
                subtitleArgs, 
                durationParam, 
                outputParam);

            return inputModifier + " " + commandLineArgs;
        }

        private string GetAudioArgs(MediaSourceInfo mediaSource)
        {
            var mediaStreams = mediaSource.MediaStreams ?? new List<MediaStream>();
            var inputAudioCodec = mediaStreams.Where(i => i.Type == MediaStreamType.Audio).Select(i => i.Codec).FirstOrDefault() ?? string.Empty;

            // do not copy aac because many players have difficulty with aac_latm
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
            if (string.Equals(_liveTvOptions.RecordedVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

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
                    _logger.Info("Stopping ffmpeg recording process for {0}", _targetPath);

                    //process.Kill();
                    _process.StandardInput.WriteLine("q");
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error stopping recording transcoding job for {0}", ex, _targetPath);
                }

                if (_hasExited)
                {
                    return;
                }

                try
                {
                    _logger.Info("Calling recording process.WaitForExit for {0}", _targetPath);

                    if (_process.WaitForExit(10000))
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error waiting for recording process to exit for {0}", ex, _targetPath);
                }

                if (_hasExited)
                {
                    return;
                }

                try
                {
                    _logger.Info("Killing ffmpeg recording process for {0}", _targetPath);

                    _process.Kill();
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error killing recording transcoding job for {0}", ex, _targetPath);
                }
            }
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        private void OnFfMpegProcessExited(IProcess process, string inputFile)
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