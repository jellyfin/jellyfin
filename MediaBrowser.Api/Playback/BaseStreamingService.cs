using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Diagnostics;

namespace MediaBrowser.Api.Playback
{
    /// <summary>
    /// Class BaseStreamingService
    /// </summary>
    public abstract class BaseStreamingService : BaseApiService
    {
        /// <summary>
        /// Gets or sets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        protected IServerConfigurationManager ServerConfigurationManager { get; private set; }

        /// <summary>
        /// Gets or sets the user manager.
        /// </summary>
        /// <value>The user manager.</value>
        protected IUserManager UserManager { get; private set; }

        /// <summary>
        /// Gets or sets the library manager.
        /// </summary>
        /// <value>The library manager.</value>
        protected ILibraryManager LibraryManager { get; private set; }

        /// <summary>
        /// Gets or sets the iso manager.
        /// </summary>
        /// <value>The iso manager.</value>
        protected IIsoManager IsoManager { get; private set; }

        /// <summary>
        /// Gets or sets the media encoder.
        /// </summary>
        /// <value>The media encoder.</value>
        protected IMediaEncoder MediaEncoder { get; private set; }

        protected IFileSystem FileSystem { get; private set; }

        protected IDlnaManager DlnaManager { get; private set; }
        protected IDeviceManager DeviceManager { get; private set; }
        protected ISubtitleEncoder SubtitleEncoder { get; private set; }
        protected IMediaSourceManager MediaSourceManager { get; private set; }
        protected IZipClient ZipClient { get; private set; }
        protected IJsonSerializer JsonSerializer { get; private set; }

        public static IServerApplicationHost AppHost;
        public static IHttpClient HttpClient;
        protected IAuthorizationContext AuthorizationContext { get; private set; }

        protected EncodingHelper EncodingHelper { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStreamingService" /> class.
        /// </summary>
        protected BaseStreamingService(IServerConfigurationManager serverConfig, IUserManager userManager, ILibraryManager libraryManager, IIsoManager isoManager, IMediaEncoder mediaEncoder, IFileSystem fileSystem, IDlnaManager dlnaManager, ISubtitleEncoder subtitleEncoder, IDeviceManager deviceManager, IMediaSourceManager mediaSourceManager, IZipClient zipClient, IJsonSerializer jsonSerializer, IAuthorizationContext authorizationContext)
        {
            JsonSerializer = jsonSerializer;
            AuthorizationContext = authorizationContext;
            ZipClient = zipClient;
            MediaSourceManager = mediaSourceManager;
            DeviceManager = deviceManager;
            SubtitleEncoder = subtitleEncoder;
            DlnaManager = dlnaManager;
            FileSystem = fileSystem;
            ServerConfigurationManager = serverConfig;
            UserManager = userManager;
            LibraryManager = libraryManager;
            IsoManager = isoManager;
            MediaEncoder = mediaEncoder;
            EncodingHelper = new EncodingHelper(MediaEncoder, serverConfig, FileSystem, SubtitleEncoder);
        }

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        /// <param name="outputPath">The output path.</param>
        /// <param name="state">The state.</param>
        /// <param name="isEncoding">if set to <c>true</c> [is encoding].</param>
        /// <returns>System.String.</returns>
        protected abstract string GetCommandLineArguments(string outputPath, StreamState state, bool isEncoding);

        /// <summary>
        /// Gets the type of the transcoding job.
        /// </summary>
        /// <value>The type of the transcoding job.</value>
        protected abstract TranscodingJobType TranscodingJobType { get; }

        /// <summary>
        /// Gets the output file extension.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <returns>System.String.</returns>
        protected virtual string GetOutputFileExtension(StreamState state)
        {
            return Path.GetExtension(state.RequestedUrl);
        }

        /// <summary>
        /// Gets the output file path.
        /// </summary>
        private string GetOutputFilePath(StreamState state, string outputFileExtension)
        {
            var folder = ServerConfigurationManager.ApplicationPaths.TranscodingTempPath;

            var data = GetCommandLineArguments("dummy\\dummy", state, false);

            data += "-" + (state.Request.DeviceId ?? string.Empty);
            data += "-" + (state.Request.PlaySessionId ?? string.Empty);

            var dataHash = data.GetMD5().ToString("N");

            if (EnableOutputInSubFolder)
            {
                return Path.Combine(folder, dataHash, dataHash + (outputFileExtension ?? string.Empty).ToLower());
            }

            return Path.Combine(folder, dataHash + (outputFileExtension ?? string.Empty).ToLower());
        }

        protected virtual bool EnableOutputInSubFolder
        {
            get { return false; }
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

        protected virtual string GetDefaultH264Preset()
        {
            return "superfast";
        }

        private async Task AcquireResources(StreamState state, CancellationTokenSource cancellationTokenSource)
        {
            if (state.VideoType == VideoType.Iso && state.IsoType.HasValue && IsoManager.CanMount(state.MediaPath))
            {
                state.IsoMount = await IsoManager.Mount(state.MediaPath, cancellationTokenSource.Token).ConfigureAwait(false);
            }

            if (state.MediaSource.RequiresOpening && string.IsNullOrWhiteSpace(state.Request.LiveStreamId))
            {
                var liveStreamResponse = await MediaSourceManager.OpenLiveStream(new LiveStreamRequest
                {
                    OpenToken = state.MediaSource.OpenToken

                }, false, cancellationTokenSource.Token).ConfigureAwait(false);

                EncodingHelper.AttachMediaSourceInfo(state, liveStreamResponse.MediaSource, state.RequestedUrl);

                if (state.VideoRequest != null)
                {
                    EncodingHelper.TryStreamCopy(state);
                }
            }

            if (state.MediaSource.BufferMs.HasValue)
            {
                await Task.Delay(state.MediaSource.BufferMs.Value, cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Starts the FFMPEG.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="cancellationTokenSource">The cancellation token source.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <returns>Task.</returns>
        protected async Task<TranscodingJob> StartFfMpeg(StreamState state,
            string outputPath,
            CancellationTokenSource cancellationTokenSource,
            string workingDirectory = null)
        {
            FileSystem.CreateDirectory(Path.GetDirectoryName(outputPath));

            await AcquireResources(state, cancellationTokenSource).ConfigureAwait(false);

            if (state.VideoRequest != null && !string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                var auth = AuthorizationContext.GetAuthorizationInfo(Request);
                if (!string.IsNullOrWhiteSpace(auth.UserId))
                {
                    var user = UserManager.GetUserById(auth.UserId);
                    if (!user.Policy.EnableVideoPlaybackTranscoding)
                    {
                        ApiEntryPoint.Instance.OnTranscodeFailedToStart(outputPath, TranscodingJobType, state);

                        throw new ArgumentException("User does not have access to video transcoding");
                    }
                }
            }

            var transcodingId = Guid.NewGuid().ToString("N");
            var commandLineArgs = GetCommandLineArguments(outputPath, state, true);

            var process = ApiEntryPoint.Instance.ProcessFactory.Create(new ProcessOptions
            {
                CreateNoWindow = true,
                UseShellExecute = false,

                // Must consume both stdout and stderr or deadlocks may occur
                //RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,

                FileName = MediaEncoder.EncoderPath,
                Arguments = commandLineArgs,

                IsHidden = true,
                ErrorDialog = false,
                EnableRaisingEvents = true,
                WorkingDirectory = !string.IsNullOrWhiteSpace(workingDirectory) ? workingDirectory : null
            });

            var transcodingJob = ApiEntryPoint.Instance.OnTranscodeBeginning(outputPath,
                state.Request.PlaySessionId,
                state.MediaSource.LiveStreamId,
                transcodingId,
                TranscodingJobType,
                process,
                state.Request.DeviceId,
                state,
                cancellationTokenSource);

            var commandLineLogMessage = process.StartInfo.FileName + " " + process.StartInfo.Arguments;
            Logger.Info(commandLineLogMessage);

            var logFilePrefix = "ffmpeg-transcode";
            if (state.VideoRequest != null && string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase) && string.Equals(state.OutputAudioCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                logFilePrefix = "ffmpeg-directstream";
            }
            else if (state.VideoRequest != null && string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
            {
                logFilePrefix = "ffmpeg-remux";
            }

            var logFilePath = Path.Combine(ServerConfigurationManager.ApplicationPaths.LogDirectoryPath, logFilePrefix + "-" + Guid.NewGuid() + ".txt");
            FileSystem.CreateDirectory(Path.GetDirectoryName(logFilePath));

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            state.LogFileStream = FileSystem.GetFileStream(logFilePath, FileOpenMode.Create, FileAccessMode.Write, FileShareMode.Read, true);

            var commandLineLogMessageBytes = Encoding.UTF8.GetBytes(Request.AbsoluteUri + Environment.NewLine + Environment.NewLine + JsonSerializer.SerializeToString(state.MediaSource) + Environment.NewLine + Environment.NewLine + commandLineLogMessage + Environment.NewLine + Environment.NewLine);
            await state.LogFileStream.WriteAsync(commandLineLogMessageBytes, 0, commandLineLogMessageBytes.Length, cancellationTokenSource.Token).ConfigureAwait(false);

            process.Exited += (sender, args) => OnFfMpegProcessExited(process, transcodingJob, state);

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error starting ffmpeg", ex);

                ApiEntryPoint.Instance.OnTranscodeFailedToStart(outputPath, TranscodingJobType, state);

                throw;
            }

            // MUST read both stdout and stderr asynchronously or a deadlock may occurr
            //process.BeginOutputReadLine();

            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            var task = Task.Run(() => StartStreamingLog(transcodingJob, state, process.StandardError.BaseStream, state.LogFileStream));

            // Wait for the file to exist before proceeeding
            while (!FileSystem.FileExists(state.WaitForPath ?? outputPath) && !transcodingJob.HasExited)
            {
                await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(false);
            }

            if (state.IsInputVideo && transcodingJob.Type == TranscodingJobType.Progressive && !transcodingJob.HasExited)
            {
                await Task.Delay(1000, cancellationTokenSource.Token).ConfigureAwait(false);

                if (state.ReadInputAtNativeFramerate && !transcodingJob.HasExited)
                {
                    await Task.Delay(1500, cancellationTokenSource.Token).ConfigureAwait(false);
                }
            }

            if (!transcodingJob.HasExited)
            {
                StartThrottler(state, transcodingJob);
            }

            ReportUsage(state);

            return transcodingJob;
        }

        private void StartThrottler(StreamState state, TranscodingJob transcodingJob)
        {
            if (EnableThrottling(state))
            {
                transcodingJob.TranscodingThrottler = state.TranscodingThrottler = new TranscodingThrottler(transcodingJob, Logger, ServerConfigurationManager, ApiEntryPoint.Instance.TimerFactory, FileSystem);
                state.TranscodingThrottler.Start();
            }
        }

        private bool EnableThrottling(StreamState state)
        {
            return false;
            //// do not use throttling with hardware encoders
            //return state.InputProtocol == MediaProtocol.File &&
            //    state.RunTimeTicks.HasValue &&
            //    state.RunTimeTicks.Value >= TimeSpan.FromMinutes(5).Ticks &&
            //    state.IsInputVideo &&
            //    state.VideoType == VideoType.VideoFile &&
            //    !string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase) &&
            //    string.Equals(GetVideoEncoder(state), "libx264", StringComparison.OrdinalIgnoreCase);
        }

        private async Task StartStreamingLog(TranscodingJob transcodingJob, StreamState state, Stream source, Stream target)
        {
            try
            {
                using (var reader = new StreamReader(source))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = await reader.ReadLineAsync().ConfigureAwait(false);

                        ParseLogLine(line, transcodingJob, state);

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
                Logger.ErrorException("Error reading ffmpeg log", ex);
            }
        }

        private void ParseLogLine(string line, TranscodingJob transcodingJob, StreamState state)
        {
            float? framerate = null;
            double? percent = null;
            TimeSpan? transcodingPosition = null;
            long? bytesTranscoded = null;
            int? bitRate = null;

            var parts = line.Split(' ');

            var totalMs = state.RunTimeTicks.HasValue
                ? TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalMilliseconds
                : 0;

            var startMs = state.Request.StartTimeTicks.HasValue
                ? TimeSpan.FromTicks(state.Request.StartTimeTicks.Value).TotalMilliseconds
                : 0;

            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];

                if (string.Equals(part, "fps=", StringComparison.OrdinalIgnoreCase) &&
                    (i + 1 < parts.Length))
                {
                    var rate = parts[i + 1];
                    float val;

                    if (float.TryParse(rate, NumberStyles.Any, UsCulture, out val))
                    {
                        framerate = val;
                    }
                }
                else if (state.RunTimeTicks.HasValue &&
                    part.StartsWith("time=", StringComparison.OrdinalIgnoreCase))
                {
                    var time = part.Split(new[] { '=' }, 2).Last();
                    TimeSpan val;

                    if (TimeSpan.TryParse(time, UsCulture, out val))
                    {
                        var currentMs = startMs + val.TotalMilliseconds;

                        var percentVal = currentMs / totalMs;
                        percent = 100 * percentVal;

                        transcodingPosition = val;
                    }
                }
                else if (part.StartsWith("size=", StringComparison.OrdinalIgnoreCase))
                {
                    var size = part.Split(new[] { '=' }, 2).Last();

                    int? scale = null;
                    if (size.IndexOf("kb", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        scale = 1024;
                        size = size.Replace("kb", string.Empty, StringComparison.OrdinalIgnoreCase);
                    }

                    if (scale.HasValue)
                    {
                        long val;

                        if (long.TryParse(size, NumberStyles.Any, UsCulture, out val))
                        {
                            bytesTranscoded = val * scale.Value;
                        }
                    }
                }
                else if (part.StartsWith("bitrate=", StringComparison.OrdinalIgnoreCase))
                {
                    var rate = part.Split(new[] { '=' }, 2).Last();

                    int? scale = null;
                    if (rate.IndexOf("kbits/s", StringComparison.OrdinalIgnoreCase) != -1)
                    {
                        scale = 1024;
                        rate = rate.Replace("kbits/s", string.Empty, StringComparison.OrdinalIgnoreCase);
                    }

                    if (scale.HasValue)
                    {
                        float val;

                        if (float.TryParse(rate, NumberStyles.Any, UsCulture, out val))
                        {
                            bitRate = (int)Math.Ceiling(val * scale.Value);
                        }
                    }
                }
            }

            if (framerate.HasValue || percent.HasValue)
            {
                ApiEntryPoint.Instance.ReportTranscodingProgress(transcodingJob, state, transcodingPosition, framerate, percent, bytesTranscoded, bitRate);
            }
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="job">The job.</param>
        /// <param name="state">The state.</param>
        private void OnFfMpegProcessExited(IProcess process, TranscodingJob job, StreamState state)
        {
            if (job != null)
            {
                job.HasExited = true;
            }

            Logger.Debug("Disposing stream resources");
            state.Dispose();

            try
            {
                Logger.Info("FFMpeg exited with code {0}", process.ExitCode);
            }
            catch
            {
                Logger.Error("FFMpeg exited with an error.");
            }

            // This causes on exited to be called twice:
            //try
            //{
            //    // Dispose the process
            //    process.Dispose();
            //}
            //catch (Exception ex)
            //{
            //    Logger.ErrorException("Error disposing ffmpeg.", ex);
            //}
        }

        /// <summary>
        /// Parses the parameters.
        /// </summary>
        /// <param name="request">The request.</param>
        private void ParseParams(StreamRequest request)
        {
            var vals = request.Params.Split(';');

            var videoRequest = request as VideoStreamRequest;

            for (var i = 0; i < vals.Length; i++)
            {
                var val = vals[i];

                if (string.IsNullOrWhiteSpace(val))
                {
                    continue;
                }

                if (i == 0)
                {
                    request.DeviceProfileId = val;
                }
                else if (i == 1)
                {
                    request.DeviceId = val;
                }
                else if (i == 2)
                {
                    request.MediaSourceId = val;
                }
                else if (i == 3)
                {
                    request.Static = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                }
                else if (i == 4)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.VideoCodec = val;
                    }
                }
                else if (i == 5)
                {
                    request.AudioCodec = val;
                }
                else if (i == 6)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.AudioStreamIndex = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 7)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.SubtitleStreamIndex = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 8)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.VideoBitRate = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 9)
                {
                    request.AudioBitRate = int.Parse(val, UsCulture);
                }
                else if (i == 10)
                {
                    request.MaxAudioChannels = int.Parse(val, UsCulture);
                }
                else if (i == 11)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxFramerate = float.Parse(val, UsCulture);
                    }
                }
                else if (i == 12)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxWidth = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 13)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxHeight = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 14)
                {
                    request.StartTimeTicks = long.Parse(val, UsCulture);
                }
                else if (i == 15)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.Level = val;
                    }
                }
                else if (i == 16)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxRefFrames = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 17)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.MaxVideoBitDepth = int.Parse(val, UsCulture);
                    }
                }
                else if (i == 18)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.Profile = val;
                    }
                }
                else if (i == 19)
                {
                    // cabac no longer used
                }
                else if (i == 20)
                {
                    request.PlaySessionId = val;
                }
                else if (i == 21)
                {
                    // api_key
                }
                else if (i == 22)
                {
                    request.LiveStreamId = val;
                }
                else if (i == 23)
                {
                    // Duplicating ItemId because of MediaMonkey
                }
                else if (i == 24)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.CopyTimestamps = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    }
                }
                else if (i == 25)
                {
                    if (!string.IsNullOrWhiteSpace(val) && videoRequest != null)
                    {
                        SubtitleDeliveryMethod method;
                        if (Enum.TryParse(val, out method))
                        {
                            videoRequest.SubtitleMethod = method;
                        }
                    }
                }
                else if (i == 26)
                {
                    request.TranscodingMaxAudioChannels = int.Parse(val, UsCulture);
                }
                else if (i == 27)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.EnableSubtitlesInManifest = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    }
                }
                else if (i == 28)
                {
                    request.Tag = val;
                }
                else if (i == 29)
                {
                    if (videoRequest != null)
                    {
                        videoRequest.RequireAvc = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                    }
                }
                else if (i == 30)
                {
                    request.SubtitleCodec = val;
                }
            }
        }

        /// <summary>
        /// Parses the dlna headers.
        /// </summary>
        /// <param name="request">The request.</param>
        private void ParseDlnaHeaders(StreamRequest request)
        {
            if (!request.StartTimeTicks.HasValue)
            {
                var timeSeek = GetHeader("TimeSeekRange.dlna.org");

                request.StartTimeTicks = ParseTimeSeekHeader(timeSeek);
            }
        }

        /// <summary>
        /// Parses the time seek header.
        /// </summary>
        private long? ParseTimeSeekHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (value.IndexOf("npt=", StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new ArgumentException("Invalid timeseek header");
            }
            value = value.Substring(4).Split(new[] { '-' }, 2)[0];

            if (value.IndexOf(':') == -1)
            {
                // Parses npt times in the format of '417.33'
                double seconds;
                if (double.TryParse(value, NumberStyles.Any, UsCulture, out seconds))
                {
                    return TimeSpan.FromSeconds(seconds).Ticks;
                }

                throw new ArgumentException("Invalid timeseek header");
            }

            // Parses npt times in the format of '10:19:25.7'
            var tokens = value.Split(new[] { ':' }, 3);
            double secondsSum = 0;
            var timeFactor = 3600;

            foreach (var time in tokens)
            {
                double digit;
                if (double.TryParse(time, NumberStyles.Any, UsCulture, out digit))
                {
                    secondsSum += digit * timeFactor;
                }
                else
                {
                    throw new ArgumentException("Invalid timeseek header");
                }
                timeFactor /= 60;
            }
            return TimeSpan.FromSeconds(secondsSum).Ticks;
        }

        /// <summary>
        /// Gets the state.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>StreamState.</returns>
        protected async Task<StreamState> GetState(StreamRequest request, CancellationToken cancellationToken)
        {
            ParseDlnaHeaders(request);

            if (!string.IsNullOrWhiteSpace(request.Params))
            {
                ParseParams(request);
            }

            var url = Request.PathInfo;

            if (string.IsNullOrEmpty(request.AudioCodec))
            {
                request.AudioCodec = EncodingHelper.InferAudioCodec(url);
            }

            var state = new StreamState(MediaSourceManager, Logger, TranscodingJobType)
            {
                Request = request,
                RequestedUrl = url,
                UserAgent = Request.UserAgent
            };

            var auth = AuthorizationContext.GetAuthorizationInfo(Request);
            if (!string.IsNullOrWhiteSpace(auth.UserId))
            {
                state.User = UserManager.GetUserById(auth.UserId);
            }

            //if ((Request.UserAgent ?? string.Empty).IndexOf("iphone", StringComparison.OrdinalIgnoreCase) != -1 ||
            //    (Request.UserAgent ?? string.Empty).IndexOf("ipad", StringComparison.OrdinalIgnoreCase) != -1 ||
            //    (Request.UserAgent ?? string.Empty).IndexOf("ipod", StringComparison.OrdinalIgnoreCase) != -1)
            //{
            //    state.SegmentLength = 6;
            //}

            if (state.VideoRequest != null)
            {
                if (!string.IsNullOrWhiteSpace(state.VideoRequest.VideoCodec))
                {
                    state.SupportedVideoCodecs = state.VideoRequest.VideoCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                    state.VideoRequest.VideoCodec = state.SupportedVideoCodecs.FirstOrDefault();
                }
            }

            if (!string.IsNullOrWhiteSpace(request.AudioCodec))
            {
                state.SupportedAudioCodecs = request.AudioCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                state.Request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault(i => MediaEncoder.CanEncodeToAudioCodec(i))
                    ?? state.SupportedAudioCodecs.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(request.SubtitleCodec))
            {
                state.SupportedSubtitleCodecs = request.SubtitleCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                state.Request.SubtitleCodec = state.SupportedSubtitleCodecs.FirstOrDefault(i => MediaEncoder.CanEncodeToSubtitleCodec(i))
                    ?? state.SupportedSubtitleCodecs.FirstOrDefault();
            }

            var item = LibraryManager.GetItemById(request.Id);

            state.IsInputVideo = string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);

            MediaSourceInfo mediaSource = null;
            if (string.IsNullOrWhiteSpace(request.LiveStreamId))
            {
                TranscodingJob currentJob = !string.IsNullOrWhiteSpace(request.PlaySessionId) ?
                    ApiEntryPoint.Instance.GetTranscodingJob(request.PlaySessionId)
                    : null;

                if (currentJob != null)
                {
                    mediaSource = currentJob.MediaSource;
                }

                if (mediaSource == null)
                {
                    var mediaSources = (await MediaSourceManager.GetPlayackMediaSources(request.Id, null, false, new[] { MediaType.Audio, MediaType.Video }, cancellationToken).ConfigureAwait(false)).ToList();

                    mediaSource = string.IsNullOrEmpty(request.MediaSourceId)
                       ? mediaSources.First()
                       : mediaSources.FirstOrDefault(i => string.Equals(i.Id, request.MediaSourceId));

                    if (mediaSource == null && string.Equals(request.Id, request.MediaSourceId, StringComparison.OrdinalIgnoreCase))
                    {
                        mediaSource = mediaSources.First();
                    }
                }
            }
            else
            {
                var liveStreamInfo = await MediaSourceManager.GetLiveStreamWithDirectStreamProvider(request.LiveStreamId, cancellationToken).ConfigureAwait(false);
                mediaSource = liveStreamInfo.Item1;
                state.DirectStreamProvider = liveStreamInfo.Item2;
            }

            var videoRequest = request as VideoStreamRequest;

            EncodingHelper.AttachMediaSourceInfo(state, mediaSource, url);

            var container = Path.GetExtension(state.RequestedUrl);

            if (string.IsNullOrEmpty(container))
            {
                container = request.Container;
            }

            if (string.IsNullOrEmpty(container))
            {
                container = request.Static ?
                    state.InputContainer :
                    GetOutputFileExtension(state);
            }

            state.OutputContainer = (container ?? string.Empty).TrimStart('.');

            state.OutputAudioBitrate = EncodingHelper.GetAudioBitrateParam(state.Request, state.AudioStream);
            state.OutputAudioSampleRate = request.AudioSampleRate;

            state.OutputAudioCodec = state.Request.AudioCodec;

            state.OutputAudioChannels = EncodingHelper.GetNumAudioChannelsParam(state.Request, state.AudioStream, state.OutputAudioCodec);

            if (videoRequest != null)
            {
                state.OutputVideoCodec = state.VideoRequest.VideoCodec;
                state.OutputVideoBitrate = EncodingHelper.GetVideoBitrateParamValue(state.VideoRequest, state.VideoStream, state.OutputVideoCodec);

                if (videoRequest != null)
                {
                    EncodingHelper.TryStreamCopy(state);
                }

                if (state.OutputVideoBitrate.HasValue && !string.Equals(state.OutputVideoCodec, "copy", StringComparison.OrdinalIgnoreCase))
                {
                    var resolution = ResolutionNormalizer.Normalize(
                        state.VideoStream == null ? (int?)null : state.VideoStream.BitRate,
                        state.OutputVideoBitrate.Value,
                        state.VideoStream == null ? null : state.VideoStream.Codec,
                        state.OutputVideoCodec,
                        videoRequest.MaxWidth,
                        videoRequest.MaxHeight);

                    videoRequest.MaxWidth = resolution.MaxWidth;
                    videoRequest.MaxHeight = resolution.MaxHeight;
                }

                ApplyDeviceProfileSettings(state);
            }
            else
            {
                ApplyDeviceProfileSettings(state);
            }

            var ext = string.IsNullOrWhiteSpace(state.OutputContainer)
                ? GetOutputFileExtension(state)
                : ("." + state.OutputContainer);
            state.OutputFilePath = GetOutputFilePath(state, ext);

            return state;
        }

        private void ApplyDeviceProfileSettings(StreamState state)
        {
            var headers = Request.Headers.ToDictionary();

            if (!string.IsNullOrWhiteSpace(state.Request.DeviceProfileId))
            {
                state.DeviceProfile = DlnaManager.GetProfile(state.Request.DeviceProfileId);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(state.Request.DeviceId))
                {
                    var caps = DeviceManager.GetCapabilities(state.Request.DeviceId);

                    if (caps != null)
                    {
                        state.DeviceProfile = caps.DeviceProfile;
                    }
                    else
                    {
                        state.DeviceProfile = DlnaManager.GetProfile(headers);
                    }
                }
            }

            var profile = state.DeviceProfile;

            if (profile == null)
            {
                // Don't use settings from the default profile. 
                // Only use a specific profile if it was requested.
                return;
            }

            var audioCodec = state.ActualOutputAudioCodec;
            var videoCodec = state.ActualOutputVideoCodec;

            var mediaProfile = state.VideoRequest == null ?
                profile.GetAudioMediaProfile(state.OutputContainer, audioCodec, state.OutputAudioChannels, state.OutputAudioBitrate) :
                profile.GetVideoMediaProfile(state.OutputContainer,
                audioCodec,
                videoCodec,
                state.OutputWidth,
                state.OutputHeight,
                state.TargetVideoBitDepth,
                state.OutputVideoBitrate,
                state.TargetVideoProfile,
                state.TargetVideoLevel,
                state.TargetFramerate,
                state.TargetPacketLength,
                state.TargetTimestamp,
                state.IsTargetAnamorphic,
                state.TargetRefFrames,
                state.TargetVideoStreamCount,
                state.TargetAudioStreamCount,
                state.TargetVideoCodecTag,
                state.IsTargetAVC);

            if (mediaProfile != null)
            {
                state.MimeType = mediaProfile.MimeType;
            }

            if (!state.Request.Static)
            {
                var transcodingProfile = state.VideoRequest == null ?
                    profile.GetAudioTranscodingProfile(state.OutputContainer, audioCodec) :
                    profile.GetVideoTranscodingProfile(state.OutputContainer, audioCodec, videoCodec);

                if (transcodingProfile != null)
                {
                    state.EstimateContentLength = transcodingProfile.EstimateContentLength;
                    state.EnableMpegtsM2TsMode = transcodingProfile.EnableMpegtsM2TsMode;
                    state.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;

                    if (state.VideoRequest != null)
                    {
                        state.VideoRequest.CopyTimestamps = transcodingProfile.CopyTimestamps;
                        state.VideoRequest.EnableSubtitlesInManifest = transcodingProfile.EnableSubtitlesInManifest;
                    }
                }
            }
        }

        private async void ReportUsage(StreamState state)
        {
            try
            {
                await ReportUsageInternal(state).ConfigureAwait(false);
            }
            catch
            {

            }
        }

        private Task ReportUsageInternal(StreamState state)
        {
            if (!ServerConfigurationManager.Configuration.EnableAnonymousUsageReporting)
            {
                return Task.FromResult(true);
            }

            if (!MediaEncoder.IsDefaultEncoderPath)
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(true);

            //var dict = new Dictionary<string, string>();

            //var outputAudio = GetAudioEncoder(state);
            //if (!string.IsNullOrWhiteSpace(outputAudio))
            //{
            //    dict["outputAudio"] = outputAudio;
            //}

            //var outputVideo = GetVideoEncoder(state);
            //if (!string.IsNullOrWhiteSpace(outputVideo))
            //{
            //    dict["outputVideo"] = outputVideo;
            //}

            //if (ServerConfigurationManager.Configuration.CodecsUsed.Contains(outputAudio ?? string.Empty, StringComparer.OrdinalIgnoreCase) &&
            //    ServerConfigurationManager.Configuration.CodecsUsed.Contains(outputVideo ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            //{
            //    return Task.FromResult(true);
            //}

            //dict["id"] = AppHost.SystemId;
            //dict["type"] = state.VideoRequest == null ? "Audio" : "Video";

            //var audioStream = state.AudioStream;
            //if (audioStream != null && !string.IsNullOrWhiteSpace(audioStream.Codec))
            //{
            //    dict["inputAudio"] = audioStream.Codec;
            //}

            //var videoStream = state.VideoStream;
            //if (videoStream != null && !string.IsNullOrWhiteSpace(videoStream.Codec))
            //{
            //    dict["inputVideo"] = videoStream.Codec;
            //}

            //var cert = GetType().Assembly.GetModules().First().GetSignerCertificate();
            //if (cert != null)
            //{
            //    dict["assemblySig"] = cert.GetCertHashString();
            //    dict["certSubject"] = cert.Subject ?? string.Empty;
            //    dict["certIssuer"] = cert.Issuer ?? string.Empty;
            //}
            //else
            //{
            //    return Task.FromResult(true);
            //}

            //if (state.SupportedAudioCodecs.Count > 0)
            //{
            //    dict["supportedAudioCodecs"] = string.Join(",", state.SupportedAudioCodecs.ToArray());
            //}

            //var auth = AuthorizationContext.GetAuthorizationInfo(Request);

            //dict["appName"] = auth.Client ?? string.Empty;
            //dict["appVersion"] = auth.Version ?? string.Empty;
            //dict["device"] = auth.Device ?? string.Empty;
            //dict["deviceId"] = auth.DeviceId ?? string.Empty;
            //dict["context"] = "streaming";

            ////Logger.Info(JsonSerializer.SerializeToString(dict));
            //if (!ServerConfigurationManager.Configuration.CodecsUsed.Contains(outputAudio ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            //{
            //    var list = ServerConfigurationManager.Configuration.CodecsUsed.ToList();
            //    list.Add(outputAudio);
            //    ServerConfigurationManager.Configuration.CodecsUsed = list.ToArray();
            //}

            //if (!ServerConfigurationManager.Configuration.CodecsUsed.Contains(outputVideo ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            //{
            //    var list = ServerConfigurationManager.Configuration.CodecsUsed.ToList();
            //    list.Add(outputVideo);
            //    ServerConfigurationManager.Configuration.CodecsUsed = list.ToArray();
            //}

            //ServerConfigurationManager.SaveConfiguration();

            ////Logger.Info(JsonSerializer.SerializeToString(dict));
            //var options = new HttpRequestOptions()
            //{
            //    Url = "https://mb3admin.com/admin/service/transcoding/report",
            //    CancellationToken = CancellationToken.None,
            //    LogRequest = false,
            //    LogErrors = false,
            //    BufferContent = false
            //};
            //options.RequestContent = JsonSerializer.SerializeToString(dict);
            //options.RequestContentType = "application/json";

            //return HttpClient.Post(options);
        }

        /// <summary>
        /// Adds the dlna headers.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <param name="isStaticallyStreamed">if set to <c>true</c> [is statically streamed].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected void AddDlnaHeaders(StreamState state, IDictionary<string, string> responseHeaders, bool isStaticallyStreamed)
        {
            var profile = state.DeviceProfile;

            var transferMode = GetHeader("transferMode.dlna.org");
            responseHeaders["transferMode.dlna.org"] = string.IsNullOrEmpty(transferMode) ? "Streaming" : transferMode;
            responseHeaders["realTimeInfo.dlna.org"] = "DLNA.ORG_TLAG=*";

            if (string.Equals(GetHeader("getMediaInfo.sec"), "1", StringComparison.OrdinalIgnoreCase))
            {
                if (state.RunTimeTicks.HasValue)
                {
                    var ms = TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalMilliseconds;
                    responseHeaders["MediaInfo.sec"] = string.Format("SEC_Duration={0};", Convert.ToInt32(ms).ToString(CultureInfo.InvariantCulture));
                }
            }

            if (state.RunTimeTicks.HasValue && !isStaticallyStreamed && profile != null)
            {
                AddTimeSeekResponseHeaders(state, responseHeaders);
            }

            if (profile == null)
            {
                profile = DlnaManager.GetDefaultProfile();
            }

            var audioCodec = state.ActualOutputAudioCodec;

            if (state.VideoRequest == null)
            {
                responseHeaders["contentFeatures.dlna.org"] = new ContentFeatureBuilder(profile)
                    .BuildAudioHeader(
                    state.OutputContainer,
                    audioCodec,
                    state.OutputAudioBitrate,
                    state.OutputAudioSampleRate,
                    state.OutputAudioChannels,
                    isStaticallyStreamed,
                    state.RunTimeTicks,
                    state.TranscodeSeekInfo
                    );
            }
            else
            {
                var videoCodec = state.ActualOutputVideoCodec;

                responseHeaders["contentFeatures.dlna.org"] = new ContentFeatureBuilder(profile)
                    .BuildVideoHeader(
                    state.OutputContainer,
                    videoCodec,
                    audioCodec,
                    state.OutputWidth,
                    state.OutputHeight,
                    state.TargetVideoBitDepth,
                    state.OutputVideoBitrate,
                    state.TargetTimestamp,
                    isStaticallyStreamed,
                    state.RunTimeTicks,
                    state.TargetVideoProfile,
                    state.TargetVideoLevel,
                    state.TargetFramerate,
                    state.TargetPacketLength,
                    state.TranscodeSeekInfo,
                    state.IsTargetAnamorphic,
                    state.TargetRefFrames,
                    state.TargetVideoStreamCount,
                    state.TargetAudioStreamCount,
                    state.TargetVideoCodecTag,
                    state.IsTargetAVC

                    ).FirstOrDefault() ?? string.Empty;
            }

            foreach (var item in responseHeaders)
            {
                Request.Response.AddHeader(item.Key, item.Value);
            }
        }

        private void AddTimeSeekResponseHeaders(StreamState state, IDictionary<string, string> responseHeaders)
        {
            var runtimeSeconds = TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalSeconds.ToString(UsCulture);
            var startSeconds = TimeSpan.FromTicks(state.Request.StartTimeTicks ?? 0).TotalSeconds.ToString(UsCulture);

            responseHeaders["TimeSeekRange.dlna.org"] = string.Format("npt={0}-{1}/{1}", startSeconds, runtimeSeconds);
            responseHeaders["X-AvailableSeekRange"] = string.Format("1 npt={0}-{1}", startSeconds, runtimeSeconds);
        }
    }
}
