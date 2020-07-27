using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The audio controller.
    /// </summary>
    // TODO: In order to autheneticate this in the future, Dlna playback will require updating
    public class AudioController : BaseJellyfinApiController
    {
        private readonly IDlnaManager _dlnaManager;
        private readonly IAuthorizationContext _authContext;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IFileSystem _fileSystem;
        private readonly ISubtitleEncoder _subtitleEncoder;
        private readonly IConfiguration _configuration;
        private readonly IDeviceManager _deviceManager;
        private readonly TranscodingJobHelper _transcodingJobHelper;
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly TranscodingJobType _transcodingJobType = TranscodingJobType.Progressive;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioController"/> class.
        /// </summary>
        /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
        /// <param name="userManger">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="authorizationContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="subtitleEncoder">Instance of the <see cref="ISubtitleEncoder"/> interface.</param>
        /// <param name="configuration">Instance of the <see cref="IConfiguration"/> interface.</param>
        /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
        /// <param name="transcodingJobHelper">The <see cref="TranscodingJobHelper"/> singleton.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        public AudioController(
            IDlnaManager dlnaManager,
            IUserManager userManger,
            IAuthorizationContext authorizationContext,
            ILibraryManager libraryManager,
            IMediaSourceManager mediaSourceManager,
            IServerConfigurationManager serverConfigurationManager,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            ISubtitleEncoder subtitleEncoder,
            IConfiguration configuration,
            IDeviceManager deviceManager,
            TranscodingJobHelper transcodingJobHelper,
            IHttpClientFactory httpClientFactory)
        {
            _dlnaManager = dlnaManager;
            _authContext = authorizationContext;
            _userManager = userManger;
            _libraryManager = libraryManager;
            _mediaSourceManager = mediaSourceManager;
            _serverConfigurationManager = serverConfigurationManager;
            _mediaEncoder = mediaEncoder;
            _fileSystem = fileSystem;
            _subtitleEncoder = subtitleEncoder;
            _configuration = configuration;
            _deviceManager = deviceManager;
            _transcodingJobHelper = transcodingJobHelper;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Gets an audio stream.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="container">The audio container.</param>
        /// <param name="static">Optional. If true, the original file will be streamed statically without any encoding. Use either no url extension or the original file extension. true/false.</param>
        /// <param name="params">The streaming parameters.</param>
        /// <param name="tag">The tag.</param>
        /// <param name="deviceProfileId">Optional. The dlna device profile id to utilize.</param>
        /// <param name="playSessionId">The play session id.</param>
        /// <param name="segmentContainer">The segment container.</param>
        /// <param name="segmentLength">The segment lenght.</param>
        /// <param name="minSegments">The minimum number of segments.</param>
        /// <param name="mediaSourceId">The media version id, if playing an alternate version.</param>
        /// <param name="deviceId">The device id of the client requesting. Used to stop encoding processes when needed.</param>
        /// <param name="audioCodec">Optional. Specify a audio codec to encode to, e.g. mp3. If omitted the server will auto-select using the url's extension. Options: aac, mp3, vorbis, wma.</param>
        /// <param name="enableAutoStreamCopy">Whether or not to allow automatic stream copy if requested values match the original source. Defaults to true.</param>
        /// <param name="allowVideoStreamCopy">Whether or not to allow copying of the video stream url.</param>
        /// <param name="allowAudioStreamCopy">Whether or not to allow copying of the audio stream url.</param>
        /// <param name="breakOnNonKeyFrames">Optional. Whether to break on non key frames.</param>
        /// <param name="audioSampleRate">Optional. Specify a specific audio sample rate, e.g. 44100.</param>
        /// <param name="maxAudioBitDepth">Optional. The maximum audio bit depth.</param>
        /// <param name="audioBitRate">Optional. Specify an audio bitrate to encode to, e.g. 128000. If omitted this will be left to encoder defaults.</param>
        /// <param name="audioChannels">Optional. Specify a specific number of audio channels to encode to, e.g. 2.</param>
        /// <param name="maxAudioChannels">Optional. Specify a maximum number of audio channels to encode to, e.g. 2.</param>
        /// <param name="profile">Optional. Specify a specific an encoder profile (varies by encoder), e.g. main, baseline, high.</param>
        /// <param name="level">Optional. Specify a level for the encoder profile (varies by encoder), e.g. 3, 3.1.</param>
        /// <param name="framerate">Optional. A specific video framerate to encode to, e.g. 23.976. Generally this should be omitted unless the device has specific requirements.</param>
        /// <param name="maxFramerate">Optional. A specific maximum video framerate to encode to, e.g. 23.976. Generally this should be omitted unless the device has specific requirements.</param>
        /// <param name="copyTimestamps">Whether or not to copy timestamps when transcoding with an offset. Defaults to false.</param>
        /// <param name="startTimeTicks">Optional. Specify a starting offset, in ticks. 1 tick = 10000 ms.</param>
        /// <param name="width">Optional. The fixed horizontal resolution of the encoded video.</param>
        /// <param name="height">Optional. The fixed vertical resolution of the encoded video.</param>
        /// <param name="videoBitRate">Optional. Specify a video bitrate to encode to, e.g. 500000. If omitted this will be left to encoder defaults.</param>
        /// <param name="subtitleStreamIndex">Optional. The index of the subtitle stream to use. If omitted no subtitles will be used.</param>
        /// <param name="subtitleMethod">Optional. Specify the subtitle delivery method.</param>
        /// <param name="maxRefFrames">Optional.</param>
        /// <param name="maxVideoBitDepth">Optional. The maximum video bit depth.</param>
        /// <param name="requireAvc">Optional. Whether to require avc.</param>
        /// <param name="deInterlace">Optional. Whether to deinterlace the video.</param>
        /// <param name="requireNonAnamorphic">Optional. Whether to require a non anamporphic stream.</param>
        /// <param name="transcodingMaxAudioChannels">Optional. The maximum number of audio channels to transcode.</param>
        /// <param name="cpuCoreLimit">Optional. The limit of how many cpu cores to use.</param>
        /// <param name="liveStreamId">The live stream id.</param>
        /// <param name="enableMpegtsM2TsMode">Optional. Whether to enable the MpegtsM2Ts mode.</param>
        /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264. If omitted the server will auto-select using the url's extension. Options: h265, h264, mpeg4, theora, vpx, wmv.</param>
        /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
        /// <param name="transcodingReasons">Optional. The transcoding reason.</param>
        /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
        /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
        /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
        /// <param name="streamOptions">Optional. The streaming options.</param>
        /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
        [HttpGet("{itemId}/{stream=stream}.{container?}")]
        [HttpGet("{itemId}/stream")]
        [HttpHead("{itemId}/{stream=stream}.{container?}")]
        [HttpHead("{itemId}/stream")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> GetAudioStream(
            [FromRoute] Guid itemId,
            [FromRoute] string? container,
            [FromQuery] bool? @static,
            [FromQuery] string? @params,
            [FromQuery] string? tag,
            [FromQuery] string? deviceProfileId,
            [FromQuery] string? playSessionId,
            [FromQuery] string? segmentContainer,
            [FromQuery] int? segmentLength,
            [FromQuery] int? minSegments,
            [FromQuery] string? mediaSourceId,
            [FromQuery] string? deviceId,
            [FromQuery] string? audioCodec,
            [FromQuery] bool? enableAutoStreamCopy,
            [FromQuery] bool? allowVideoStreamCopy,
            [FromQuery] bool? allowAudioStreamCopy,
            [FromQuery] bool? breakOnNonKeyFrames,
            [FromQuery] int? audioSampleRate,
            [FromQuery] int? maxAudioBitDepth,
            [FromQuery] int? audioBitRate,
            [FromQuery] int? audioChannels,
            [FromQuery] int? maxAudioChannels,
            [FromQuery] string? profile,
            [FromQuery] string? level,
            [FromQuery] float? framerate,
            [FromQuery] float? maxFramerate,
            [FromQuery] bool? copyTimestamps,
            [FromQuery] long? startTimeTicks,
            [FromQuery] int? width,
            [FromQuery] int? height,
            [FromQuery] int? videoBitRate,
            [FromQuery] int? subtitleStreamIndex,
            [FromQuery] SubtitleDeliveryMethod subtitleMethod,
            [FromQuery] int? maxRefFrames,
            [FromQuery] int? maxVideoBitDepth,
            [FromQuery] bool? requireAvc,
            [FromQuery] bool? deInterlace,
            [FromQuery] bool? requireNonAnamorphic,
            [FromQuery] int? transcodingMaxAudioChannels,
            [FromQuery] int? cpuCoreLimit,
            [FromQuery] string? liveStreamId,
            [FromQuery] bool? enableMpegtsM2TsMode,
            [FromQuery] string? videoCodec,
            [FromQuery] string? subtitleCodec,
            [FromQuery] string? transcodingReasons,
            [FromQuery] int? audioStreamIndex,
            [FromQuery] int? videoStreamIndex,
            [FromQuery] EncodingContext context,
            [FromQuery] Dictionary<string, string> streamOptions)
        {
            bool isHeadRequest = Request.Method == System.Net.WebRequestMethods.Http.Head;

            using var cancellationTokenSource = new CancellationTokenSource();

            StreamingRequestDto streamingRequest = new StreamingRequestDto
            {
                Id = itemId,
                Container = container,
                Static = @static ?? true,
                Params = @params,
                Tag = tag,
                DeviceProfileId = deviceProfileId,
                PlaySessionId = playSessionId,
                SegmentContainer = segmentContainer,
                SegmentLength = segmentLength,
                MinSegments = minSegments,
                MediaSourceId = mediaSourceId,
                DeviceId = deviceId,
                AudioCodec = audioCodec,
                EnableAutoStreamCopy = enableAutoStreamCopy ?? true,
                AllowAudioStreamCopy = allowAudioStreamCopy ?? true,
                AllowVideoStreamCopy = allowVideoStreamCopy ?? true,
                BreakOnNonKeyFrames = breakOnNonKeyFrames ?? false,
                AudioSampleRate = audioSampleRate,
                MaxAudioChannels = maxAudioChannels,
                AudioBitRate = audioBitRate,
                MaxAudioBitDepth = maxAudioBitDepth,
                AudioChannels = audioChannels,
                Profile = profile,
                Level = level,
                Framerate = framerate,
                MaxFramerate = maxFramerate,
                CopyTimestamps = copyTimestamps ?? true,
                StartTimeTicks = startTimeTicks,
                Width = width,
                Height = height,
                VideoBitRate = videoBitRate,
                SubtitleStreamIndex = subtitleStreamIndex,
                SubtitleMethod = subtitleMethod,
                MaxRefFrames = maxRefFrames,
                MaxVideoBitDepth = maxVideoBitDepth,
                RequireAvc = requireAvc ?? true,
                DeInterlace = deInterlace ?? true,
                RequireNonAnamorphic = requireNonAnamorphic ?? true,
                TranscodingMaxAudioChannels = transcodingMaxAudioChannels,
                CpuCoreLimit = cpuCoreLimit,
                LiveStreamId = liveStreamId,
                EnableMpegtsM2TsMode = enableMpegtsM2TsMode ?? true,
                VideoCodec = videoCodec,
                SubtitleCodec = subtitleCodec,
                TranscodeReasons = transcodingReasons,
                AudioStreamIndex = audioStreamIndex,
                VideoStreamIndex = videoStreamIndex,
                Context = context,
                StreamOptions = streamOptions
            };

            using var state = await StreamingHelpers.GetStreamingState(
                    streamingRequest,
                    Request,
                    _authContext,
                    _mediaSourceManager,
                    _userManager,
                    _libraryManager,
                    _serverConfigurationManager,
                    _mediaEncoder,
                    _fileSystem,
                    _subtitleEncoder,
                    _configuration,
                    _dlnaManager,
                    _deviceManager,
                    _transcodingJobHelper,
                    _transcodingJobType,
                    cancellationTokenSource.Token)
                .ConfigureAwait(false);

            if (@static.HasValue && @static.Value && state.DirectStreamProvider != null)
            {
                StreamingHelpers.AddDlnaHeaders(state, Response.Headers, true, startTimeTicks, Request, _dlnaManager);

                await new ProgressiveFileCopier(state.DirectStreamProvider, null, _transcodingJobHelper, CancellationToken.None)
                    {
                        AllowEndOfFile = false
                    }.WriteToAsync(Response.Body, CancellationToken.None)
                    .ConfigureAwait(false);

                // TODO (moved from MediaBrowser.Api): Don't hardcode contentType
                return File(Response.Body, MimeTypes.GetMimeType("file.ts")!);
            }

            // Static remote stream
            if (@static.HasValue && @static.Value && state.InputProtocol == MediaProtocol.Http)
            {
                StreamingHelpers.AddDlnaHeaders(state, Response.Headers, true, startTimeTicks, Request, _dlnaManager);

                using var httpClient = _httpClientFactory.CreateClient();
                return await FileStreamResponseHelpers.GetStaticRemoteStreamResult(state, isHeadRequest, this, httpClient).ConfigureAwait(false);
            }

            if (@static.HasValue && @static.Value && state.InputProtocol != MediaProtocol.File)
            {
                return BadRequest($"Input protocol {state.InputProtocol} cannot be streamed statically");
            }

            var outputPath = state.OutputFilePath;
            var outputPathExists = System.IO.File.Exists(outputPath);

            var transcodingJob = _transcodingJobHelper.GetTranscodingJob(outputPath, TranscodingJobType.Progressive);
            var isTranscodeCached = outputPathExists && transcodingJob != null;

            StreamingHelpers.AddDlnaHeaders(state, Response.Headers, (@static.HasValue && @static.Value) || isTranscodeCached, startTimeTicks, Request, _dlnaManager);

            // Static stream
            if (@static.HasValue && @static.Value)
            {
                var contentType = state.GetMimeType("." + state.OutputContainer, false) ?? state.GetMimeType(state.MediaPath);

                if (state.MediaSource.IsInfiniteStream)
                {
                    await new ProgressiveFileCopier(state.MediaPath, null, _transcodingJobHelper, CancellationToken.None)
                        {
                            AllowEndOfFile = false
                        }.WriteToAsync(Response.Body, CancellationToken.None)
                        .ConfigureAwait(false);

                    return File(Response.Body, contentType);
                }

                return FileStreamResponseHelpers.GetStaticFileResult(
                    state.MediaPath,
                    contentType,
                    isHeadRequest,
                    this);
            }

            // Need to start ffmpeg (because media can't be returned directly)
            var encodingOptions = _serverConfigurationManager.GetEncodingOptions();
            var encodingHelper = new EncodingHelper(_mediaEncoder, _fileSystem, _subtitleEncoder, _configuration);
            var ffmpegCommandLineArguments = encodingHelper.GetProgressiveAudioFullCommandLine(state, encodingOptions, outputPath);
            return await FileStreamResponseHelpers.GetTranscodedFile(
                state,
                isHeadRequest,
                this,
                _transcodingJobHelper,
                ffmpegCommandLineArguments,
                Request,
                _transcodingJobType,
                cancellationTokenSource).ConfigureAwait(false);
        }
    }
}
