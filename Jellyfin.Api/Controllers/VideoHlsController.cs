using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.PlaybackDtos;
using Jellyfin.Api.Models.StreamingDtos;
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
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The video hls controller.
    /// </summary>
    [Route("")]
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class VideoHlsController : BaseJellyfinApiController
    {
        private const string DefaultEncoderPreset = "superfast";
        private const TranscodingJobType TranscodingJobType = MediaBrowser.Controller.MediaEncoding.TranscodingJobType.Hls;

        private readonly EncodingHelper _encodingHelper;
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
        private readonly ILogger<VideoHlsController> _logger;
        private readonly EncodingOptions _encodingOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoHlsController"/> class.
        /// </summary>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="subtitleEncoder">Instance of the <see cref="ISubtitleEncoder"/> interface.</param>
        /// <param name="configuration">Instance of the <see cref="IConfiguration"/> interface.</param>
        /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
        /// <param name="userManger">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="authorizationContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
        /// <param name="transcodingJobHelper">The <see cref="TranscodingJobHelper"/> singleton.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{VideoHlsController}"/>.</param>
        public VideoHlsController(
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            ISubtitleEncoder subtitleEncoder,
            IConfiguration configuration,
            IDlnaManager dlnaManager,
            IUserManager userManger,
            IAuthorizationContext authorizationContext,
            ILibraryManager libraryManager,
            IMediaSourceManager mediaSourceManager,
            IServerConfigurationManager serverConfigurationManager,
            IDeviceManager deviceManager,
            TranscodingJobHelper transcodingJobHelper,
            ILogger<VideoHlsController> logger)
        {
            _encodingHelper = new EncodingHelper(mediaEncoder, fileSystem, subtitleEncoder, configuration);

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
            _logger = logger;
            _encodingOptions = serverConfigurationManager.GetEncodingOptions();
        }

        /// <summary>
        /// Gets a hls live stream.
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
        /// <param name="requireNonAnamorphic">Optional. Whether to require a non anamorphic stream.</param>
        /// <param name="transcodingMaxAudioChannels">Optional. The maximum number of audio channels to transcode.</param>
        /// <param name="cpuCoreLimit">Optional. The limit of how many cpu cores to use.</param>
        /// <param name="liveStreamId">The live stream id.</param>
        /// <param name="enableMpegtsM2TsMode">Optional. Whether to enable the MpegtsM2Ts mode.</param>
        /// <param name="videoCodec">Optional. Specify a video codec to encode to, e.g. h264. If omitted the server will auto-select using the url's extension. Options: h265, h264, mpeg4, theora, vpx, wmv.</param>
        /// <param name="subtitleCodec">Optional. Specify a subtitle codec to encode to.</param>
        /// <param name="transcodeReasons">Optional. The transcoding reason.</param>
        /// <param name="audioStreamIndex">Optional. The index of the audio stream to use. If omitted the first audio stream will be used.</param>
        /// <param name="videoStreamIndex">Optional. The index of the video stream to use. If omitted the first video stream will be used.</param>
        /// <param name="context">Optional. The <see cref="EncodingContext"/>.</param>
        /// <param name="streamOptions">Optional. The streaming options.</param>
        /// <param name="maxWidth">Optional. The max width.</param>
        /// <param name="maxHeight">Optional. The max height.</param>
        /// <param name="enableSubtitlesInManifest">Optional. Whether to enable subtitles in the manifest.</param>
        /// <response code="200">Hls live stream retrieved.</response>
        /// <returns>A <see cref="FileResult"/> containing the hls file.</returns>
        [HttpGet("Videos/{itemId}/live.m3u8")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesPlaylistFile]
        public async Task<ActionResult> GetLiveHlsStream(
            [FromRoute, Required] Guid itemId,
            [FromQuery] string? container,
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
            [FromQuery] SubtitleDeliveryMethod? subtitleMethod,
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
            [FromQuery] string? transcodeReasons,
            [FromQuery] int? audioStreamIndex,
            [FromQuery] int? videoStreamIndex,
            [FromQuery] EncodingContext? context,
            [FromQuery] Dictionary<string, string> streamOptions,
            [FromQuery] int? maxWidth,
            [FromQuery] int? maxHeight,
            [FromQuery] bool? enableSubtitlesInManifest)
        {
            VideoRequestDto streamingRequest = new VideoRequestDto
            {
                Id = itemId,
                Container = container,
                Static = @static ?? false,
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
                CopyTimestamps = copyTimestamps ?? false,
                StartTimeTicks = startTimeTicks,
                Width = width,
                Height = height,
                VideoBitRate = videoBitRate,
                SubtitleStreamIndex = subtitleStreamIndex,
                SubtitleMethod = subtitleMethod ?? SubtitleDeliveryMethod.Encode,
                MaxRefFrames = maxRefFrames,
                MaxVideoBitDepth = maxVideoBitDepth,
                RequireAvc = requireAvc ?? false,
                DeInterlace = deInterlace ?? false,
                RequireNonAnamorphic = requireNonAnamorphic ?? false,
                TranscodingMaxAudioChannels = transcodingMaxAudioChannels,
                CpuCoreLimit = cpuCoreLimit,
                LiveStreamId = liveStreamId,
                EnableMpegtsM2TsMode = enableMpegtsM2TsMode ?? false,
                VideoCodec = videoCodec,
                SubtitleCodec = subtitleCodec,
                TranscodeReasons = transcodeReasons,
                AudioStreamIndex = audioStreamIndex,
                VideoStreamIndex = videoStreamIndex,
                Context = context ?? EncodingContext.Streaming,
                StreamOptions = streamOptions,
                MaxHeight = maxHeight,
                MaxWidth = maxWidth,
                EnableSubtitlesInManifest = enableSubtitlesInManifest ?? true
            };

            var cancellationTokenSource = new CancellationTokenSource();
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
                    TranscodingJobType,
                    cancellationTokenSource.Token)
                .ConfigureAwait(false);

            TranscodingJobDto? job = null;
            var playlistPath = Path.ChangeExtension(state.OutputFilePath, ".m3u8");

            if (!System.IO.File.Exists(playlistPath))
            {
                var transcodingLock = _transcodingJobHelper.GetTranscodingLock(playlistPath);
                await transcodingLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                try
                {
                    if (!System.IO.File.Exists(playlistPath))
                    {
                        // If the playlist doesn't already exist, startup ffmpeg
                        try
                        {
                            job = await _transcodingJobHelper.StartFfMpeg(
                                    state,
                                    playlistPath,
                                    GetCommandLineArguments(playlistPath, state),
                                    Request,
                                    TranscodingJobType,
                                    cancellationTokenSource)
                                .ConfigureAwait(false);
                            job.IsLiveOutput = true;
                        }
                        catch
                        {
                            state.Dispose();
                            throw;
                        }

                        minSegments = state.MinSegments;
                        if (minSegments > 0)
                        {
                            await HlsHelpers.WaitForMinimumSegmentCount(playlistPath, minSegments, _logger, cancellationTokenSource.Token).ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    transcodingLock.Release();
                }
            }

            job ??= _transcodingJobHelper.OnTranscodeBeginRequest(playlistPath, TranscodingJobType);

            if (job != null)
            {
                _transcodingJobHelper.OnTranscodeEndRequest(job);
            }

            var playlistText = HlsHelpers.GetLivePlaylistText(playlistPath, state);

            return Content(playlistText, MimeTypes.GetMimeType("playlist.m3u8"));
        }

        /// <summary>
        /// Gets the command line arguments for ffmpeg.
        /// </summary>
        /// <param name="outputPath">The output path of the file.</param>
        /// <param name="state">The <see cref="StreamState"/>.</param>
        /// <returns>The command line arguments as a string.</returns>
        private string GetCommandLineArguments(string outputPath, StreamState state)
        {
            var videoCodec = _encodingHelper.GetVideoEncoder(state, _encodingOptions);
            var threads = EncodingHelper.GetNumberOfThreads(state, _encodingOptions, videoCodec); // GetNumberOfThreads is static.
            var inputModifier = _encodingHelper.GetInputModifier(state, _encodingOptions);
            var mapArgs = state.IsOutputVideo ? _encodingHelper.GetMapArgs(state) : string.Empty;

            var directory = Path.GetDirectoryName(outputPath) ?? throw new ArgumentException($"Provided path ({outputPath}) is not valid.", nameof(outputPath));
            var outputFileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputPath);
            var outputPrefix = Path.Combine(directory, outputFileNameWithoutExtension);
            var outputExtension = EncodingHelper.GetSegmentFileExtension(state.Request.SegmentContainer);
            var outputTsArg = outputPrefix + "%d" + outputExtension;

            var segmentFormat = outputExtension.TrimStart('.');
            if (string.Equals(segmentFormat, "ts", StringComparison.OrdinalIgnoreCase))
            {
                segmentFormat = "mpegts";
            }
            else if (string.Equals(segmentFormat, "mp4", StringComparison.OrdinalIgnoreCase))
            {
                var outputFmp4HeaderArg = string.Empty;
                var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                if (isWindows)
                {
                    // on Windows, the path of fmp4 header file needs to be configured
                    outputFmp4HeaderArg = " -hls_fmp4_init_filename \"" + outputPrefix + "-1" + outputExtension + "\"";
                }
                else
                {
                    // on Linux/Unix, ffmpeg generate fmp4 header file to m3u8 output folder
                    outputFmp4HeaderArg = " -hls_fmp4_init_filename \"" + outputFileNameWithoutExtension + "-1" + outputExtension + "\"";
                }

                segmentFormat = "fmp4" + outputFmp4HeaderArg;
            }
            else
            {
                _logger.LogError("Invalid HLS segment container: {SegmentFormat}", segmentFormat);
            }

            var maxMuxingQueueSize = _encodingOptions.MaxMuxingQueueSize > 128
                ? _encodingOptions.MaxMuxingQueueSize.ToString(CultureInfo.InvariantCulture)
                : "128";

            var baseUrlParam = string.Format(
                CultureInfo.InvariantCulture,
                "\"hls/{0}/\"",
                Path.GetFileNameWithoutExtension(outputPath));

            return string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} {1} -map_metadata -1 -map_chapters -1 -threads {2} {3} {4} {5} -copyts -avoid_negative_ts disabled -max_muxing_queue_size {6} -f hls -max_delay 5000000 -hls_time {7} -hls_segment_type {8} -start_number 0 -hls_base_url {9} -hls_playlist_type event -hls_segment_filename \"{10}\" -y \"{11}\"",
                    inputModifier,
                    _encodingHelper.GetInputArgument(state, _encodingOptions),
                    threads,
                    mapArgs,
                    GetVideoArguments(state),
                    GetAudioArguments(state),
                    maxMuxingQueueSize,
                    state.SegmentLength.ToString(CultureInfo.InvariantCulture),
                    segmentFormat,
                    baseUrlParam,
                    outputTsArg,
                    outputPath).Trim();
        }

        /// <summary>
        /// Gets the audio arguments for transcoding.
        /// </summary>
        /// <param name="state">The <see cref="StreamState"/>.</param>
        /// <returns>The command line arguments for audio transcoding.</returns>
        private string GetAudioArguments(StreamState state)
        {
            if (state.AudioStream == null)
            {
                return string.Empty;
            }

            var audioCodec = _encodingHelper.GetAudioEncoder(state);

            if (!state.IsOutputVideo)
            {
                if (EncodingHelper.IsCopyCodec(audioCodec))
                {
                    var bitStreamArgs = EncodingHelper.GetAudioBitStreamArguments(state, state.Request.SegmentContainer, state.MediaSource.Container);

                    return "-acodec copy -strict -2" + bitStreamArgs;
                }

                var audioTranscodeParams = string.Empty;

                audioTranscodeParams += "-acodec " + audioCodec;

                if (state.OutputAudioBitrate.HasValue)
                {
                    audioTranscodeParams += " -ab " + state.OutputAudioBitrate.Value.ToString(CultureInfo.InvariantCulture);
                }

                if (state.OutputAudioChannels.HasValue)
                {
                    audioTranscodeParams += " -ac " + state.OutputAudioChannels.Value.ToString(CultureInfo.InvariantCulture);
                }

                if (state.OutputAudioSampleRate.HasValue)
                {
                    audioTranscodeParams += " -ar " + state.OutputAudioSampleRate.Value.ToString(CultureInfo.InvariantCulture);
                }

                audioTranscodeParams += " -vn";
                return audioTranscodeParams;
            }

            if (EncodingHelper.IsCopyCodec(audioCodec))
            {
                var bitStreamArgs = EncodingHelper.GetAudioBitStreamArguments(state, state.Request.SegmentContainer, state.MediaSource.Container);

                return "-acodec copy -strict -2" + bitStreamArgs;
            }

            var args = "-codec:a:0 " + audioCodec;

            var channels = state.OutputAudioChannels;

            if (channels.HasValue)
            {
                args += " -ac " + channels.Value;
            }

            var bitrate = state.OutputAudioBitrate;

            if (bitrate.HasValue)
            {
                args += " -ab " + bitrate.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (state.OutputAudioSampleRate.HasValue)
            {
                args += " -ar " + state.OutputAudioSampleRate.Value.ToString(CultureInfo.InvariantCulture);
            }

            args += _encodingHelper.GetAudioFilterParam(state, _encodingOptions, true);

            return args;
        }

        /// <summary>
        /// Gets the video arguments for transcoding.
        /// </summary>
        /// <param name="state">The <see cref="StreamState"/>.</param>
        /// <returns>The command line arguments for video transcoding.</returns>
        private string GetVideoArguments(StreamState state)
        {
            if (state.VideoStream == null)
            {
                return string.Empty;
            }

            if (!state.IsOutputVideo)
            {
                return string.Empty;
            }

            var codec = _encodingHelper.GetVideoEncoder(state, _encodingOptions);

            var args = "-codec:v:0 " + codec;

            // Prefer hvc1 to hev1.
            if (string.Equals(state.ActualOutputVideoCodec, "h265", StringComparison.OrdinalIgnoreCase)
                || string.Equals(state.ActualOutputVideoCodec, "hevc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "h265", StringComparison.OrdinalIgnoreCase)
                || string.Equals(codec, "hevc", StringComparison.OrdinalIgnoreCase))
            {
                args += " -tag:v:0 hvc1";
            }

            // if (state.EnableMpegtsM2TsMode)
            // {
            //     args += " -mpegts_m2ts_mode 1";
            // }

            // See if we can save come cpu cycles by avoiding encoding.
            if (EncodingHelper.IsCopyCodec(codec))
            {
                // If h264_mp4toannexb is ever added, do not use it for live tv.
                if (state.VideoStream != null && !string.Equals(state.VideoStream.NalLengthSize, "0", StringComparison.OrdinalIgnoreCase))
                {
                    string bitStreamArgs = EncodingHelper.GetBitStreamArgs(state.VideoStream);
                    if (!string.IsNullOrEmpty(bitStreamArgs))
                    {
                        args += " " + bitStreamArgs;
                    }
                }

                args += " -start_at_zero";
            }
            else
            {
                args += _encodingHelper.GetVideoQualityParam(state, codec, _encodingOptions, DefaultEncoderPreset);

                // Set the key frame params for video encoding to match the hls segment time.
                args += _encodingHelper.GetHlsVideoKeyFrameArguments(state, codec, state.SegmentLength, true, null);

                // Currenly b-frames in libx265 breaks the FMP4-HLS playback on iOS, disable it for now.
                if (string.Equals(codec, "libx265", StringComparison.OrdinalIgnoreCase))
                {
                    args += " -bf 0";
                }

                var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;

                if (hasGraphicalSubs)
                {
                    // Graphical subs overlay and resolution params.
                    args += _encodingHelper.GetGraphicalSubtitleParam(state, _encodingOptions, codec);
                }
                else
                {
                    // Resolution params.
                    args += _encodingHelper.GetOutputSizeParam(state, _encodingOptions, codec);
                }

                if (state.SubtitleStream == null || !state.SubtitleStream.IsExternal || state.SubtitleStream.IsTextSubtitleStream)
                {
                    args += " -start_at_zero";
                }
            }

            args += " -flags -global_header";

            if (!string.IsNullOrEmpty(state.OutputVideoSync))
            {
                args += " -vsync " + state.OutputVideoSync;
            }

            args += _encodingHelper.GetOutputFFlags(state);

            return args;
        }
    }
}
