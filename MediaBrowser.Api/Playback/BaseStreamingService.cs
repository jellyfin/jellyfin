using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Playback
{
    /// <summary>
    /// Class BaseStreamingService
    /// </summary>
    public abstract class BaseStreamingService : BaseApiService
    {
        protected virtual bool EnableOutputInSubFolder => false;

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

        protected IMediaSourceManager MediaSourceManager { get; private set; }

        protected IJsonSerializer JsonSerializer { get; private set; }

        protected IAuthorizationContext AuthorizationContext { get; private set; }

        protected EncodingHelper EncodingHelper { get; set; }

        /// <summary>
        /// Gets the type of the transcoding job.
        /// </summary>
        /// <value>The type of the transcoding job.</value>
        protected abstract TranscodingJobType TranscodingJobType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseStreamingService" /> class.
        /// </summary>
        protected BaseStreamingService(
            ILogger<BaseStreamingService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IIsoManager isoManager,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            IDlnaManager dlnaManager,
            IDeviceManager deviceManager,
            IMediaSourceManager mediaSourceManager,
            IJsonSerializer jsonSerializer,
            IAuthorizationContext authorizationContext,
            EncodingHelper encodingHelper)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            UserManager = userManager;
            LibraryManager = libraryManager;
            IsoManager = isoManager;
            MediaEncoder = mediaEncoder;
            FileSystem = fileSystem;
            DlnaManager = dlnaManager;
            DeviceManager = deviceManager;
            MediaSourceManager = mediaSourceManager;
            JsonSerializer = jsonSerializer;
            AuthorizationContext = authorizationContext;

            EncodingHelper = encodingHelper;
        }

        /// <summary>
        /// Gets the command line arguments.
        /// </summary>
        protected abstract string GetCommandLineArguments(string outputPath, EncodingOptions encodingOptions, StreamState state, bool isEncoding);

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
        private string GetOutputFilePath(StreamState state, EncodingOptions encodingOptions, string outputFileExtension)
        {
            var data = $"{state.MediaPath}-{state.UserAgent}-{state.Request.DeviceId}-{state.Request.PlaySessionId}";

            var filename = data.GetMD5().ToString("N", CultureInfo.InvariantCulture);
            var ext = outputFileExtension?.ToLowerInvariant();
            var folder = ServerConfigurationManager.GetTranscodePath();

            return EnableOutputInSubFolder
                ? Path.Combine(folder, filename, filename + ext)
                : Path.Combine(folder, filename + ext);
        }

        protected virtual string GetDefaultEncoderPreset()
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
                }, cancellationTokenSource.Token).ConfigureAwait(false);

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
        protected async Task<TranscodingJob> StartFfMpeg(
            StreamState state,
            string outputPath,
            CancellationTokenSource cancellationTokenSource,
            string workingDirectory = null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            await AcquireResources(state, cancellationTokenSource).ConfigureAwait(false);

            if (state.VideoRequest != null && !EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
            {
                var auth = AuthorizationContext.GetAuthorizationInfo(Request);
                if (auth.User != null && !auth.User.Policy.EnableVideoPlaybackTranscoding)
                {
                    ApiEntryPoint.Instance.OnTranscodeFailedToStart(outputPath, TranscodingJobType, state);

                    throw new ArgumentException("User does not have access to video transcoding");
                }
            }

            var encodingOptions = ServerConfigurationManager.GetEncodingOptions();

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false,

                    // Must consume both stdout and stderr or deadlocks may occur
                    //RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,

                    FileName = MediaEncoder.EncoderPath,
                    Arguments = GetCommandLineArguments(outputPath, encodingOptions, state, true),
                    WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? null : workingDirectory,

                    ErrorDialog = false
                },
                EnableRaisingEvents = true
            };

            var transcodingJob = ApiEntryPoint.Instance.OnTranscodeBeginning(outputPath,
                state.Request.PlaySessionId,
                state.MediaSource.LiveStreamId,
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                TranscodingJobType,
                process,
                state.Request.DeviceId,
                state,
                cancellationTokenSource);

            var commandLineLogMessage = process.StartInfo.FileName + " " + process.StartInfo.Arguments;
            Logger.LogInformation(commandLineLogMessage);

            var logFilePrefix = "ffmpeg-transcode";
            if (state.VideoRequest != null
                && EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
            {
                logFilePrefix = EncodingHelper.IsCopyCodec(state.OutputAudioCodec)
                    ? "ffmpeg-remux" : "ffmpeg-directstream";
            }

            var logFilePath = Path.Combine(ServerConfigurationManager.ApplicationPaths.LogDirectoryPath, logFilePrefix + "-" + Guid.NewGuid() + ".txt");

            // FFMpeg writes debug/error info to stderr. This is useful when debugging so let's put it in the log directory.
            Stream logStream = new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, true);

            var commandLineLogMessageBytes = Encoding.UTF8.GetBytes(Request.AbsoluteUri + Environment.NewLine + Environment.NewLine + JsonSerializer.SerializeToString(state.MediaSource) + Environment.NewLine + Environment.NewLine + commandLineLogMessage + Environment.NewLine + Environment.NewLine);
            await logStream.WriteAsync(commandLineLogMessageBytes, 0, commandLineLogMessageBytes.Length, cancellationTokenSource.Token).ConfigureAwait(false);

            process.Exited += (sender, args) => OnFfMpegProcessExited(process, transcodingJob, state);

            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error starting ffmpeg");

                ApiEntryPoint.Instance.OnTranscodeFailedToStart(outputPath, TranscodingJobType, state);

                throw;
            }

            Logger.LogDebug("Launched ffmpeg process");
            state.TranscodingJob = transcodingJob;

            // Important - don't await the log task or we won't be able to kill ffmpeg when the user stops playback
            _ = new JobLogger(Logger).StartStreamingLog(state, process.StandardError.BaseStream, logStream);

            // Wait for the file to exist before proceeeding
            var ffmpegTargetFile = state.WaitForPath ?? outputPath;
            Logger.LogDebug("Waiting for the creation of {0}", ffmpegTargetFile);
            while (!File.Exists(ffmpegTargetFile) && !transcodingJob.HasExited)
            {
                await Task.Delay(100, cancellationTokenSource.Token).ConfigureAwait(false);
            }

            Logger.LogDebug("File {0} created or transcoding has finished", ffmpegTargetFile);

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
            Logger.LogDebug("StartFfMpeg() finished successfully");

            return transcodingJob;
        }

        private void StartThrottler(StreamState state, TranscodingJob transcodingJob)
        {
            if (EnableThrottling(state))
            {
                transcodingJob.TranscodingThrottler = state.TranscodingThrottler = new TranscodingThrottler(transcodingJob, Logger, ServerConfigurationManager, FileSystem);
                state.TranscodingThrottler.Start();
            }
        }

        private bool EnableThrottling(StreamState state)
        {
            var encodingOptions = ServerConfigurationManager.GetEncodingOptions();

            // enable throttling when NOT using hardware acceleration
            if (string.IsNullOrEmpty(encodingOptions.HardwareAccelerationType))
            {
                return state.InputProtocol == MediaProtocol.File &&
                       state.RunTimeTicks.HasValue &&
                       state.RunTimeTicks.Value >= TimeSpan.FromMinutes(5).Ticks &&
                       state.IsInputVideo &&
                       state.VideoType == VideoType.VideoFile &&
                       !EncodingHelper.IsCopyCodec(state.OutputVideoCodec);
            }

            return false;
        }

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="job">The job.</param>
        /// <param name="state">The state.</param>
        private void OnFfMpegProcessExited(Process process, TranscodingJob job, StreamState state)
        {
            if (job != null)
            {
                job.HasExited = true;
            }

            Logger.LogDebug("Disposing stream resources");
            state.Dispose();

            if (process.ExitCode == 0)
            {
                Logger.LogInformation("FFMpeg exited with code 0");
            }
            else
            {
                Logger.LogError("FFMpeg exited with code {0}", process.ExitCode);
            }

            process.Dispose();
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

                switch (i)
                {
                    case 0:
                        request.DeviceProfileId = val;
                        break;
                    case 1:
                        request.DeviceId = val;
                        break;
                    case 2:
                        request.MediaSourceId = val;
                        break;
                    case 3:
                        request.Static = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        break;
                    case 4:
                        if (videoRequest != null)
                        {
                            videoRequest.VideoCodec = val;
                        }

                        break;
                    case 5:
                        request.AudioCodec = val;
                        break;
                    case 6:
                        if (videoRequest != null)
                        {
                            videoRequest.AudioStreamIndex = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 7:
                        if (videoRequest != null)
                        {
                            videoRequest.SubtitleStreamIndex = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 8:
                        if (videoRequest != null)
                        {
                            videoRequest.VideoBitRate = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 9:
                        request.AudioBitRate = int.Parse(val, CultureInfo.InvariantCulture);
                        break;
                    case 10:
                        request.MaxAudioChannels = int.Parse(val, CultureInfo.InvariantCulture);
                        break;
                    case 11:
                        if (videoRequest != null)
                        {
                            videoRequest.MaxFramerate = float.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 12:
                        if (videoRequest != null)
                        {
                            videoRequest.MaxWidth = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 13:
                        if (videoRequest != null)
                        {
                            videoRequest.MaxHeight = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 14:
                        request.StartTimeTicks = long.Parse(val, CultureInfo.InvariantCulture);
                        break;
                    case 15:
                        if (videoRequest != null)
                        {
                            videoRequest.Level = val;
                        }

                        break;
                    case 16:
                        if (videoRequest != null)
                        {
                            videoRequest.MaxRefFrames = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 17:
                        if (videoRequest != null)
                        {
                            videoRequest.MaxVideoBitDepth = int.Parse(val, CultureInfo.InvariantCulture);
                        }

                        break;
                    case 18:
                        if (videoRequest != null)
                        {
                            videoRequest.Profile = val;
                        }

                        break;
                    case 19:
                        // cabac no longer used
                        break;
                    case 20:
                        request.PlaySessionId = val;
                        break;
                    case 21:
                        // api_key
                        break;
                    case 22:
                        request.LiveStreamId = val;
                        break;
                    case 23:
                        // Duplicating ItemId because of MediaMonkey
                        break;
                    case 24:
                        if (videoRequest != null)
                        {
                            videoRequest.CopyTimestamps = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }

                        break;
                    case 25:
                        if (!string.IsNullOrWhiteSpace(val) && videoRequest != null)
                        {
                            if (Enum.TryParse(val, out SubtitleDeliveryMethod method))
                            {
                                videoRequest.SubtitleMethod = method;
                            }
                        }

                        break;
                    case 26:
                        request.TranscodingMaxAudioChannels = int.Parse(val, CultureInfo.InvariantCulture);
                        break;
                    case 27:
                        if (videoRequest != null)
                        {
                            videoRequest.EnableSubtitlesInManifest = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }

                        break;
                    case 28:
                        request.Tag = val;
                        break;
                    case 29:
                        if (videoRequest != null)
                        {
                            videoRequest.RequireAvc = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }

                        break;
                    case 30:
                        request.SubtitleCodec = val;
                        break;
                    case 31:
                        if (videoRequest != null)
                        {
                            videoRequest.RequireNonAnamorphic = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }

                        break;
                    case 32:
                        if (videoRequest != null)
                        {
                            videoRequest.DeInterlace = string.Equals("true", val, StringComparison.OrdinalIgnoreCase);
                        }

                        break;
                    case 33:
                        request.TranscodeReasons = val;
                        break;
                }
            }
        }

        /// <summary>
        /// Parses query parameters as StreamOptions.
        /// </summary>
        /// <param name="request">The stream request.</param>
        private void ParseStreamOptions(StreamRequest request)
        {
            foreach (var param in Request.QueryString)
            {
                if (char.IsLower(param.Key[0]))
                {
                    // This was probably not parsed initially and should be a StreamOptions
                    // TODO: This should be incorporated either in the lower framework for parsing requests
                    // or the generated URL should correctly serialize it
                    request.StreamOptions[param.Key] = param.Value;
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

            const string Npt = "npt=";
            if (!value.StartsWith(Npt, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid timeseek header");
            }
            int index = value.IndexOf('-');
            value = index == -1
                ? value.Substring(Npt.Length)
                : value.Substring(Npt.Length, index - Npt.Length);

            if (value.IndexOf(':') == -1)
            {
                // Parses npt times in the format of '417.33'
                if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var seconds))
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
                if (double.TryParse(time, NumberStyles.Any, CultureInfo.InvariantCulture, out var digit))
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

            ParseStreamOptions(request);

            var url = Request.PathInfo;

            if (string.IsNullOrEmpty(request.AudioCodec))
            {
                request.AudioCodec = EncodingHelper.InferAudioCodec(url);
            }

            var enableDlnaHeaders = !string.IsNullOrWhiteSpace(request.Params) ||
                                    string.Equals(GetHeader("GetContentFeatures.DLNA.ORG"), "1", StringComparison.OrdinalIgnoreCase);

            var state = new StreamState(MediaSourceManager, TranscodingJobType)
            {
                Request = request,
                RequestedUrl = url,
                UserAgent = Request.UserAgent,
                EnableDlnaHeaders = enableDlnaHeaders
            };

            var auth = AuthorizationContext.GetAuthorizationInfo(Request);
            if (!auth.UserId.Equals(Guid.Empty))
            {
                state.User = UserManager.GetUserById(auth.UserId);
            }

            //if ((Request.UserAgent ?? string.Empty).IndexOf("iphone", StringComparison.OrdinalIgnoreCase) != -1 ||
            //    (Request.UserAgent ?? string.Empty).IndexOf("ipad", StringComparison.OrdinalIgnoreCase) != -1 ||
            //    (Request.UserAgent ?? string.Empty).IndexOf("ipod", StringComparison.OrdinalIgnoreCase) != -1)
            //{
            //    state.SegmentLength = 6;
            //}

            if (state.VideoRequest != null && !string.IsNullOrWhiteSpace(state.VideoRequest.VideoCodec))
            {
                state.SupportedVideoCodecs = state.VideoRequest.VideoCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
                state.VideoRequest.VideoCodec = state.SupportedVideoCodecs.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(request.AudioCodec))
            {
                state.SupportedAudioCodecs = request.AudioCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
                state.Request.AudioCodec = state.SupportedAudioCodecs.FirstOrDefault(i => MediaEncoder.CanEncodeToAudioCodec(i))
                    ?? state.SupportedAudioCodecs.FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(request.SubtitleCodec))
            {
                state.SupportedSubtitleCodecs = request.SubtitleCodec.Split(',').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
                state.Request.SubtitleCodec = state.SupportedSubtitleCodecs.FirstOrDefault(i => MediaEncoder.CanEncodeToSubtitleCodec(i))
                    ?? state.SupportedSubtitleCodecs.FirstOrDefault();
            }

            var item = LibraryManager.GetItemById(request.Id);

            state.IsInputVideo = string.Equals(item.MediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase);

            //var primaryImage = item.GetImageInfo(ImageType.Primary, 0) ??
            //             item.Parents.Select(i => i.GetImageInfo(ImageType.Primary, 0)).FirstOrDefault(i => i != null);
            //if (primaryImage != null)
            //{
            //    state.AlbumCoverPath = primaryImage.Path;
            //}

            MediaSourceInfo mediaSource = null;
            if (string.IsNullOrWhiteSpace(request.LiveStreamId))
            {
                var currentJob = !string.IsNullOrWhiteSpace(request.PlaySessionId) ?
                    ApiEntryPoint.Instance.GetTranscodingJob(request.PlaySessionId)
                    : null;

                if (currentJob != null)
                {
                    mediaSource = currentJob.MediaSource;
                }

                if (mediaSource == null)
                {
                    var mediaSources = await MediaSourceManager.GetPlaybackMediaSources(LibraryManager.GetItemById(request.Id), null, false, false, cancellationToken).ConfigureAwait(false);

                    mediaSource = string.IsNullOrEmpty(request.MediaSourceId)
                       ? mediaSources[0]
                       : mediaSources.Find(i => string.Equals(i.Id, request.MediaSourceId));

                    if (mediaSource == null && Guid.Parse(request.MediaSourceId) == request.Id)
                    {
                        mediaSource = mediaSources[0];
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
                    StreamBuilder.NormalizeMediaSourceFormatIntoSingleContainer(state.InputContainer, state.MediaPath, null, DlnaProfileType.Audio) :
                    GetOutputFileExtension(state);
            }

            state.OutputContainer = (container ?? string.Empty).TrimStart('.');

            state.OutputAudioBitrate = EncodingHelper.GetAudioBitrateParam(state.Request, state.AudioStream);

            state.OutputAudioCodec = state.Request.AudioCodec;

            state.OutputAudioChannels = EncodingHelper.GetNumAudioChannelsParam(state, state.AudioStream, state.OutputAudioCodec);

            if (videoRequest != null)
            {
                state.OutputVideoCodec = state.VideoRequest.VideoCodec;
                state.OutputVideoBitrate = EncodingHelper.GetVideoBitrateParamValue(state.VideoRequest, state.VideoStream, state.OutputVideoCodec);

                if (videoRequest != null)
                {
                    EncodingHelper.TryStreamCopy(state);
                }

                if (state.OutputVideoBitrate.HasValue && !EncodingHelper.IsCopyCodec(state.OutputVideoCodec))
                {
                    var resolution = ResolutionNormalizer.Normalize(
                        state.VideoStream?.BitRate,
                        state.VideoStream?.Width,
                        state.VideoStream?.Height,
                        state.OutputVideoBitrate.Value,
                        state.VideoStream?.Codec,
                        state.OutputVideoCodec,
                        videoRequest.MaxWidth,
                        videoRequest.MaxHeight);

                    videoRequest.MaxWidth = resolution.MaxWidth;
                    videoRequest.MaxHeight = resolution.MaxHeight;
                }
            }

            ApplyDeviceProfileSettings(state);

            var ext = string.IsNullOrWhiteSpace(state.OutputContainer)
                ? GetOutputFileExtension(state)
                : ('.' + state.OutputContainer);

            var encodingOptions = ServerConfigurationManager.GetEncodingOptions();

            state.OutputFilePath = GetOutputFilePath(state, encodingOptions, ext);

            return state;
        }

        private void ApplyDeviceProfileSettings(StreamState state)
        {
            var headers = Request.Headers;

            if (!string.IsNullOrWhiteSpace(state.Request.DeviceProfileId))
            {
                state.DeviceProfile = DlnaManager.GetProfile(state.Request.DeviceProfileId);
            }
            else if (!string.IsNullOrWhiteSpace(state.Request.DeviceId))
            {
                var caps = DeviceManager.GetCapabilities(state.Request.DeviceId);

                state.DeviceProfile = caps == null ? DlnaManager.GetProfile(headers) : caps.DeviceProfile;
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
                profile.GetAudioMediaProfile(state.OutputContainer, audioCodec, state.OutputAudioChannels, state.OutputAudioBitrate, state.OutputAudioSampleRate, state.OutputAudioBitDepth) :
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
                state.IsTargetInterlaced,
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
                    //state.EnableMpegtsM2TsMode = transcodingProfile.EnableMpegtsM2TsMode;
                    state.TranscodeSeekInfo = transcodingProfile.TranscodeSeekInfo;

                    if (state.VideoRequest != null)
                    {
                        state.VideoRequest.CopyTimestamps = transcodingProfile.CopyTimestamps;
                        state.VideoRequest.EnableSubtitlesInManifest = transcodingProfile.EnableSubtitlesInManifest;
                    }
                }
            }
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
            if (!state.EnableDlnaHeaders)
            {
                return;
            }

            var profile = state.DeviceProfile;

            var transferMode = GetHeader("transferMode.dlna.org");
            responseHeaders["transferMode.dlna.org"] = string.IsNullOrEmpty(transferMode) ? "Streaming" : transferMode;
            responseHeaders["realTimeInfo.dlna.org"] = "DLNA.ORG_TLAG=*";

            if (state.RunTimeTicks.HasValue)
            {
                if (string.Equals(GetHeader("getMediaInfo.sec"), "1", StringComparison.OrdinalIgnoreCase))
                {
                    var ms = TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalMilliseconds;
                    responseHeaders["MediaInfo.sec"] = string.Format(
                        CultureInfo.InvariantCulture,
                        "SEC_Duration={0};",
                        Convert.ToInt32(ms));
                }

                if (!isStaticallyStreamed && profile != null)
                {
                    AddTimeSeekResponseHeaders(state, responseHeaders);
                }
            }

            if (profile == null)
            {
                profile = DlnaManager.GetDefaultProfile();
            }

            var audioCodec = state.ActualOutputAudioCodec;

            if (state.VideoRequest == null)
            {
                responseHeaders["contentFeatures.dlna.org"] = new ContentFeatureBuilder(profile).BuildAudioHeader(
                    state.OutputContainer,
                    audioCodec,
                    state.OutputAudioBitrate,
                    state.OutputAudioSampleRate,
                    state.OutputAudioChannels,
                    state.OutputAudioBitDepth,
                    isStaticallyStreamed,
                    state.RunTimeTicks,
                    state.TranscodeSeekInfo);
            }
            else
            {
                var videoCodec = state.ActualOutputVideoCodec;

                responseHeaders["contentFeatures.dlna.org"] = new ContentFeatureBuilder(profile).BuildVideoHeader(
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
                    state.IsTargetInterlaced,
                    state.TargetRefFrames,
                    state.TargetVideoStreamCount,
                    state.TargetAudioStreamCount,
                    state.TargetVideoCodecTag,
                    state.IsTargetAVC).FirstOrDefault() ?? string.Empty;
            }
        }

        private void AddTimeSeekResponseHeaders(StreamState state, IDictionary<string, string> responseHeaders)
        {
            var runtimeSeconds = TimeSpan.FromTicks(state.RunTimeTicks.Value).TotalSeconds.ToString(CultureInfo.InvariantCulture);
            var startSeconds = TimeSpan.FromTicks(state.Request.StartTimeTicks ?? 0).TotalSeconds.ToString(CultureInfo.InvariantCulture);

            responseHeaders["TimeSeekRange.dlna.org"] = string.Format(
                CultureInfo.InvariantCulture,
                "npt={0}-{1}/{1}",
                startSeconds,
                runtimeSeconds);
            responseHeaders["X-AvailableSeekRange"] = string.Format(
                CultureInfo.InvariantCulture,
                "1 npt={0}-{1}",
                startSeconds,
                runtimeSeconds);
        }
    }
}
