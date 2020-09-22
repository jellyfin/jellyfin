using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.PlaybackDtos;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Common.Configuration;
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
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// Dynamic hls controller.
    /// </summary>
    [Route("")]
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class DynamicHlsController : BaseJellyfinApiController
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IAuthorizationContext _authContext;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IFileSystem _fileSystem;
        private readonly ISubtitleEncoder _subtitleEncoder;
        private readonly IConfiguration _configuration;
        private readonly IDeviceManager _deviceManager;
        private readonly TranscodingJobHelper _transcodingJobHelper;
        private readonly ILogger<DynamicHlsController> _logger;
        private readonly EncodingHelper _encodingHelper;
        private readonly DynamicHlsHelper _dynamicHlsHelper;

        private readonly TranscodingJobType _transcodingJobType = TranscodingJobType.Hls;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicHlsController"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
        /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="subtitleEncoder">Instance of the <see cref="ISubtitleEncoder"/> interface.</param>
        /// <param name="configuration">Instance of the <see cref="IConfiguration"/> interface.</param>
        /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
        /// <param name="transcodingJobHelper">Instance of the <see cref="TranscodingJobHelper"/> class.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{DynamicHlsController}"/> interface.</param>
        /// <param name="dynamicHlsHelper">Instance of <see cref="DynamicHlsHelper"/>.</param>
        public DynamicHlsController(
            ILibraryManager libraryManager,
            IUserManager userManager,
            IDlnaManager dlnaManager,
            IAuthorizationContext authContext,
            IMediaSourceManager mediaSourceManager,
            IServerConfigurationManager serverConfigurationManager,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            ISubtitleEncoder subtitleEncoder,
            IConfiguration configuration,
            IDeviceManager deviceManager,
            TranscodingJobHelper transcodingJobHelper,
            ILogger<DynamicHlsController> logger,
            DynamicHlsHelper dynamicHlsHelper)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
            _dlnaManager = dlnaManager;
            _authContext = authContext;
            _mediaSourceManager = mediaSourceManager;
            _serverConfigurationManager = serverConfigurationManager;
            _mediaEncoder = mediaEncoder;
            _fileSystem = fileSystem;
            _subtitleEncoder = subtitleEncoder;
            _configuration = configuration;
            _deviceManager = deviceManager;
            _transcodingJobHelper = transcodingJobHelper;
            _logger = logger;
            _dynamicHlsHelper = dynamicHlsHelper;

            _encodingHelper = new EncodingHelper(_mediaEncoder, _fileSystem, _subtitleEncoder, _configuration);
        }

        /// <summary>
        /// Gets a video hls playlist stream.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="container">The video container. Possible values are: ts, webm, asf, wmv, ogv, mp4, m4v, mkv, mpeg, mpg, avi, 3gp, wmv, wtv, m2ts, mov, iso, flv. </param>
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
        /// <param name="enableAdaptiveBitrateStreaming">Enable adaptive bitrate streaming.</param>
        /// <response code="200">Video stream returned.</response>
        /// <returns>A <see cref="FileResult"/> containing the playlist file.</returns>
        [HttpGet("Videos/{itemId}/master.m3u8")]
        [HttpHead("Videos/{itemId}/master.m3u8", Name = "HeadMasterHlsVideoPlaylist")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesPlaylistFile]
        public async Task<ActionResult> GetMasterHlsVideoPlaylist(
            [FromRoute, Required] Guid itemId,
            [FromRoute, Required] string container,
            [FromQuery] bool? @static,
            [FromQuery] string? @params,
            [FromQuery] string? tag,
            [FromQuery] string? deviceProfileId,
            [FromQuery] string? playSessionId,
            [FromQuery] string? segmentContainer,
            [FromQuery] int? segmentLength,
            [FromQuery] int? minSegments,
            [FromQuery, Required] string mediaSourceId,
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
            [FromQuery] Dictionary<string, string> streamOptions,
            [FromQuery] bool enableAdaptiveBitrateStreaming = true)
        {
            var streamingRequest = new HlsVideoRequestDto
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
                StreamOptions = streamOptions,
                EnableAdaptiveBitrateStreaming = enableAdaptiveBitrateStreaming
            };

            return await _dynamicHlsHelper.GetMasterHlsPlaylist(_transcodingJobType, streamingRequest, enableAdaptiveBitrateStreaming).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an audio hls playlist stream.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="container">The video container. Possible values are: ts, webm, asf, wmv, ogv, mp4, m4v, mkv, mpeg, mpg, avi, 3gp, wmv, wtv, m2ts, mov, iso, flv. </param>
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
        /// <param name="enableAdaptiveBitrateStreaming">Enable adaptive bitrate streaming.</param>
        /// <response code="200">Audio stream returned.</response>
        /// <returns>A <see cref="FileResult"/> containing the playlist file.</returns>
        [HttpGet("Audio/{itemId}/master.m3u8")]
        [HttpHead("Audio/{itemId}/master.m3u8", Name = "HeadMasterHlsAudioPlaylist")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesPlaylistFile]
        public async Task<ActionResult> GetMasterHlsAudioPlaylist(
            [FromRoute, Required] Guid itemId,
            [FromQuery, Required] string container,
            [FromQuery] bool? @static,
            [FromQuery] string? @params,
            [FromQuery] string? tag,
            [FromQuery] string? deviceProfileId,
            [FromQuery] string? playSessionId,
            [FromQuery] string? segmentContainer,
            [FromQuery] int? segmentLength,
            [FromQuery] int? minSegments,
            [FromQuery, Required] string mediaSourceId,
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
            [FromQuery] Dictionary<string, string> streamOptions,
            [FromQuery] bool enableAdaptiveBitrateStreaming = true)
        {
            var streamingRequest = new HlsAudioRequestDto
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
                StreamOptions = streamOptions,
                EnableAdaptiveBitrateStreaming = enableAdaptiveBitrateStreaming
            };

            return await _dynamicHlsHelper.GetMasterHlsPlaylist(_transcodingJobType, streamingRequest, enableAdaptiveBitrateStreaming).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a video stream using HTTP live streaming.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="container">The video container. Possible values are: ts, webm, asf, wmv, ogv, mp4, m4v, mkv, mpeg, mpg, avi, 3gp, wmv, wtv, m2ts, mov, iso, flv. </param>
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
        /// <response code="200">Video stream returned.</response>
        /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
        [HttpGet("Videos/{itemId}/main.m3u8")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesPlaylistFile]
        public async Task<ActionResult> GetVariantHlsVideoPlaylist(
            [FromRoute, Required] Guid itemId,
            [FromQuery, Required] string container,
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
            var cancellationTokenSource = new CancellationTokenSource();
            var streamingRequest = new VideoRequestDto
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

            return await GetVariantPlaylistInternal(streamingRequest, "main", cancellationTokenSource)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an audio stream using HTTP live streaming.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="container">The video container. Possible values are: ts, webm, asf, wmv, ogv, mp4, m4v, mkv, mpeg, mpg, avi, 3gp, wmv, wtv, m2ts, mov, iso, flv. </param>
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
        /// <response code="200">Audio stream returned.</response>
        /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
        [HttpGet("Audio/{itemId}/main.m3u8")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesPlaylistFile]
        public async Task<ActionResult> GetVariantHlsAudioPlaylist(
            [FromRoute, Required] Guid itemId,
            [FromQuery, Required] string container,
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
            var cancellationTokenSource = new CancellationTokenSource();
            var streamingRequest = new StreamingRequestDto
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

            return await GetVariantPlaylistInternal(streamingRequest, "main", cancellationTokenSource)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a video stream using HTTP live streaming.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="playlistId">The playlist id.</param>
        /// <param name="segmentId">The segment id.</param>
        /// <param name="container">The video container. Possible values are: ts, webm, asf, wmv, ogv, mp4, m4v, mkv, mpeg, mpg, avi, 3gp, wmv, wtv, m2ts, mov, iso, flv. </param>
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
        /// <response code="200">Video stream returned.</response>
        /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
        [HttpGet("Videos/{itemId}/hls1/{playlistId}/{segmentId}.{container}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesVideoFile]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "playlistId", Justification = "Imported from ServiceStack")]
        public async Task<ActionResult> GetHlsVideoSegment(
            [FromRoute, Required] Guid itemId,
            [FromRoute, Required] string playlistId,
            [FromRoute, Required] int segmentId,
            [FromRoute, Required] string container,
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
            var streamingRequest = new VideoRequestDto
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

            return await GetDynamicSegment(streamingRequest, segmentId)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a video stream using HTTP live streaming.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="playlistId">The playlist id.</param>
        /// <param name="segmentId">The segment id.</param>
        /// <param name="container">The video container. Possible values are: ts, webm, asf, wmv, ogv, mp4, m4v, mkv, mpeg, mpg, avi, 3gp, wmv, wtv, m2ts, mov, iso, flv. </param>
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
        /// <response code="200">Video stream returned.</response>
        /// <returns>A <see cref="FileResult"/> containing the audio file.</returns>
        [HttpGet("Audio/{itemId}/hls1/{playlistId}/{segmentId}.{container}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesAudioFile]
        [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "playlistId", Justification = "Imported from ServiceStack")]
        public async Task<ActionResult> GetHlsAudioSegment(
            [FromRoute, Required] Guid itemId,
            [FromRoute, Required] string playlistId,
            [FromRoute, Required] int segmentId,
            [FromRoute, Required] string container,
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
            var streamingRequest = new StreamingRequestDto
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

            return await GetDynamicSegment(streamingRequest, segmentId)
                .ConfigureAwait(false);
        }

        private async Task<ActionResult> GetVariantPlaylistInternal(StreamingRequestDto streamingRequest, string name, CancellationTokenSource cancellationTokenSource)
        {
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

            Response.Headers.Add(HeaderNames.Expires, "0");

            var segmentLengths = GetSegmentLengths(state);

            var builder = new StringBuilder();

            builder.AppendLine("#EXTM3U");
            builder.AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");
            builder.AppendLine("#EXT-X-VERSION:3");
            builder.AppendLine("#EXT-X-TARGETDURATION:" + Math.Ceiling(segmentLengths.Length > 0 ? segmentLengths.Max() : state.SegmentLength).ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("#EXT-X-MEDIA-SEQUENCE:0");

            var queryString = Request.QueryString;
            var index = 0;

            var segmentExtension = GetSegmentFileExtension(streamingRequest.SegmentContainer);

            foreach (var length in segmentLengths)
            {
                builder.AppendLine("#EXTINF:" + length.ToString("0.0000", CultureInfo.InvariantCulture) + ", nodesc");
                builder.AppendLine(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "hls1/{0}/{1}{2}{3}",
                        name,
                        index.ToString(CultureInfo.InvariantCulture),
                        segmentExtension,
                        queryString));

                index++;
            }

            builder.AppendLine("#EXT-X-ENDLIST");
            return new FileContentResult(Encoding.UTF8.GetBytes(builder.ToString()), MimeTypes.GetMimeType("playlist.m3u8"));
        }

        private async Task<ActionResult> GetDynamicSegment(StreamingRequestDto streamingRequest, int segmentId)
        {
            if ((streamingRequest.StartTimeTicks ?? 0) > 0)
            {
                throw new ArgumentException("StartTimeTicks is not allowed.");
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

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

            var playlistPath = Path.ChangeExtension(state.OutputFilePath, ".m3u8");

            var segmentPath = GetSegmentPath(state, playlistPath, segmentId);

            var segmentExtension = GetSegmentFileExtension(state.Request.SegmentContainer);

            TranscodingJobDto? job;

            if (System.IO.File.Exists(segmentPath))
            {
                job = _transcodingJobHelper.OnTranscodeBeginRequest(playlistPath, _transcodingJobType);
                _logger.LogDebug("returning {0} [it exists, try 1]", segmentPath);
                return await GetSegmentResult(state, playlistPath, segmentPath, segmentExtension, segmentId, job, cancellationToken).ConfigureAwait(false);
            }

            var transcodingLock = _transcodingJobHelper.GetTranscodingLock(playlistPath);
            await transcodingLock.WaitAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            var released = false;
            var startTranscoding = false;

            try
            {
                if (System.IO.File.Exists(segmentPath))
                {
                    job = _transcodingJobHelper.OnTranscodeBeginRequest(playlistPath, _transcodingJobType);
                    transcodingLock.Release();
                    released = true;
                    _logger.LogDebug("returning {0} [it exists, try 2]", segmentPath);
                    return await GetSegmentResult(state, playlistPath, segmentPath, segmentExtension, segmentId, job, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath, segmentExtension);
                    var segmentGapRequiringTranscodingChange = 24 / state.SegmentLength;

                    if (currentTranscodingIndex == null)
                    {
                        _logger.LogDebug("Starting transcoding because currentTranscodingIndex=null");
                        startTranscoding = true;
                    }
                    else if (segmentId < currentTranscodingIndex.Value)
                    {
                        _logger.LogDebug("Starting transcoding because requestedIndex={0} and currentTranscodingIndex={1}", segmentId, currentTranscodingIndex);
                        startTranscoding = true;
                    }
                    else if (segmentId - currentTranscodingIndex.Value > segmentGapRequiringTranscodingChange)
                    {
                        _logger.LogDebug("Starting transcoding because segmentGap is {0} and max allowed gap is {1}. requestedIndex={2}", segmentId - currentTranscodingIndex.Value, segmentGapRequiringTranscodingChange, segmentId);
                        startTranscoding = true;
                    }

                    if (startTranscoding)
                    {
                        // If the playlist doesn't already exist, startup ffmpeg
                        try
                        {
                            await _transcodingJobHelper.KillTranscodingJobs(streamingRequest.DeviceId, streamingRequest.PlaySessionId, p => false)
                                .ConfigureAwait(false);

                            if (currentTranscodingIndex.HasValue)
                            {
                                DeleteLastFile(playlistPath, segmentExtension, 0);
                            }

                            streamingRequest.StartTimeTicks = GetStartPositionTicks(state, segmentId);

                            state.WaitForPath = segmentPath;
                            var encodingOptions = _serverConfigurationManager.GetEncodingOptions();
                            job = await _transcodingJobHelper.StartFfMpeg(
                                state,
                                playlistPath,
                                GetCommandLineArguments(playlistPath, encodingOptions, state, true, segmentId),
                                Request,
                                _transcodingJobType,
                                cancellationTokenSource).ConfigureAwait(false);
                        }
                        catch
                        {
                            state.Dispose();
                            throw;
                        }

                        // await WaitForMinimumSegmentCount(playlistPath, 1, cancellationTokenSource.Token).ConfigureAwait(false);
                    }
                    else
                    {
                        job = _transcodingJobHelper.OnTranscodeBeginRequest(playlistPath, _transcodingJobType);
                        if (job?.TranscodingThrottler != null)
                        {
                            await job.TranscodingThrottler.UnpauseTranscoding().ConfigureAwait(false);
                        }
                    }
                }
            }
            finally
            {
                if (!released)
                {
                    transcodingLock.Release();
                }
            }

            _logger.LogDebug("returning {0} [general case]", segmentPath);
            job ??= _transcodingJobHelper.OnTranscodeBeginRequest(playlistPath, _transcodingJobType);
            return await GetSegmentResult(state, playlistPath, segmentPath, segmentExtension, segmentId, job, cancellationToken).ConfigureAwait(false);
        }

        private double[] GetSegmentLengths(StreamState state)
        {
            var result = new List<double>();

            var ticks = state.RunTimeTicks ?? 0;

            var segmentLengthTicks = TimeSpan.FromSeconds(state.SegmentLength).Ticks;

            while (ticks > 0)
            {
                var length = ticks >= segmentLengthTicks ? segmentLengthTicks : ticks;

                result.Add(TimeSpan.FromTicks(length).TotalSeconds);

                ticks -= length;
            }

            return result.ToArray();
        }

        private string GetCommandLineArguments(string outputPath, EncodingOptions encodingOptions, StreamState state, bool isEncoding, int startNumber)
        {
            var videoCodec = _encodingHelper.GetVideoEncoder(state, encodingOptions);

            var threads = _encodingHelper.GetNumberOfThreads(state, encodingOptions, videoCodec);

            if (state.BaseRequest.BreakOnNonKeyFrames)
            {
                // FIXME: this is actually a workaround, as ideally it really should be the client which decides whether non-keyframe
                //        breakpoints are supported; but current implementation always uses "ffmpeg input seeking" which is liable
                //        to produce a missing part of video stream before first keyframe is encountered, which may lead to
                //        awkward cases like a few starting HLS segments having no video whatsoever, which breaks hls.js
                _logger.LogInformation("Current HLS implementation doesn't support non-keyframe breaks but one is requested, ignoring that request");
                state.BaseRequest.BreakOnNonKeyFrames = false;
            }

            var inputModifier = _encodingHelper.GetInputModifier(state, encodingOptions);

            // If isEncoding is true we're actually starting ffmpeg
            var startNumberParam = isEncoding ? startNumber.ToString(CultureInfo.InvariantCulture) : "0";

            var mapArgs = state.IsOutputVideo ? _encodingHelper.GetMapArgs(state) : string.Empty;

            var outputTsArg = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath)) + "%d" + GetSegmentFileExtension(state.Request.SegmentContainer);

            var segmentFormat = GetSegmentFileExtension(state.Request.SegmentContainer).TrimStart('.');
            if (string.Equals(segmentFormat, "ts", StringComparison.OrdinalIgnoreCase))
            {
                segmentFormat = "mpegts";
            }

            var maxMuxingQueueSize = encodingOptions.MaxMuxingQueueSize > 128
                ? encodingOptions.MaxMuxingQueueSize.ToString(CultureInfo.InvariantCulture)
                : "128";

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1} -map_metadata -1 -map_chapters -1 -threads {2} {3} {4} {5} -copyts -avoid_negative_ts disabled -max_muxing_queue_size {6} -f hls -max_delay 5000000 -hls_time {7} -individual_header_trailer 0 -hls_segment_type {8} -start_number {9} -hls_segment_filename \"{10}\" -hls_playlist_type vod -hls_list_size 0 -y \"{11}\"",
                inputModifier,
                _encodingHelper.GetInputArgument(state, encodingOptions),
                threads,
                mapArgs,
                GetVideoArguments(state, encodingOptions, startNumber),
                GetAudioArguments(state, encodingOptions),
                maxMuxingQueueSize,
                state.SegmentLength.ToString(CultureInfo.InvariantCulture),
                segmentFormat,
                startNumberParam,
                outputTsArg,
                outputPath).Trim();
        }

        private string GetAudioArguments(StreamState state, EncodingOptions encodingOptions)
        {
            var audioCodec = _encodingHelper.GetAudioEncoder(state);

            if (!state.IsOutputVideo)
            {
                if (EncodingHelper.IsCopyCodec(audioCodec))
                {
                    return "-acodec copy";
                }

                var audioTranscodeParams = new List<string>();

                audioTranscodeParams.Add("-acodec " + audioCodec);

                if (state.OutputAudioBitrate.HasValue)
                {
                    audioTranscodeParams.Add("-ab " + state.OutputAudioBitrate.Value.ToString(CultureInfo.InvariantCulture));
                }

                if (state.OutputAudioChannels.HasValue)
                {
                    audioTranscodeParams.Add("-ac " + state.OutputAudioChannels.Value.ToString(CultureInfo.InvariantCulture));
                }

                if (state.OutputAudioSampleRate.HasValue)
                {
                    audioTranscodeParams.Add("-ar " + state.OutputAudioSampleRate.Value.ToString(CultureInfo.InvariantCulture));
                }

                audioTranscodeParams.Add("-vn");
                return string.Join(' ', audioTranscodeParams);
            }

            if (EncodingHelper.IsCopyCodec(audioCodec))
            {
                var videoCodec = _encodingHelper.GetVideoEncoder(state, encodingOptions);

                if (EncodingHelper.IsCopyCodec(videoCodec) && state.EnableBreakOnNonKeyFrames(videoCodec))
                {
                    return "-codec:a:0 copy -copypriorss:a:0 0";
                }

                return "-codec:a:0 copy";
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

            args += " " + _encodingHelper.GetAudioFilterParam(state, encodingOptions, true);

            return args;
        }

        private string GetVideoArguments(StreamState state, EncodingOptions encodingOptions, int startNumber)
        {
            if (!state.IsOutputVideo)
            {
                return string.Empty;
            }

            var codec = _encodingHelper.GetVideoEncoder(state, encodingOptions);

            var args = "-codec:v:0 " + codec;

            // if (state.EnableMpegtsM2TsMode)
            // {
            //     args += " -mpegts_m2ts_mode 1";
            // }

            // See if we can save come cpu cycles by avoiding encoding
            if (EncodingHelper.IsCopyCodec(codec))
            {
                if (state.VideoStream != null && !string.Equals(state.VideoStream.NalLengthSize, "0", StringComparison.OrdinalIgnoreCase))
                {
                    string bitStreamArgs = _encodingHelper.GetBitStreamArgs(state.VideoStream);
                    if (!string.IsNullOrEmpty(bitStreamArgs))
                    {
                        args += " " + bitStreamArgs;
                    }
                }

                // args += " -flags -global_header";
            }
            else
            {
                var gopArg = string.Empty;
                var keyFrameArg = string.Format(
                    CultureInfo.InvariantCulture,
                    " -force_key_frames:0 \"expr:gte(t,{0}+n_forced*{1})\"",
                    startNumber * state.SegmentLength,
                    state.SegmentLength);

                var framerate = state.VideoStream?.RealFrameRate;

                if (framerate.HasValue)
                {
                    // This is to make sure keyframe interval is limited to our segment,
                    // as forcing keyframes is not enough.
                    // Example: we encoded half of desired length, then codec detected
                    // scene cut and inserted a keyframe; next forced keyframe would
                    // be created outside of segment, which breaks seeking
                    // -sc_threshold 0 is used to prevent the hardware encoder from post processing to break the set keyframe
                    gopArg = string.Format(
                        CultureInfo.InvariantCulture,
                        " -g {0} -keyint_min {0} -sc_threshold 0",
                        Math.Ceiling(state.SegmentLength * framerate.Value));
                }

                args += " " + _encodingHelper.GetVideoQualityParam(state, codec, encodingOptions, "veryfast");

                // Unable to force key frames using these hw encoders, set key frames by GOP
                if (string.Equals(codec, "h264_qsv", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(codec, "h264_nvenc", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(codec, "h264_amf", StringComparison.OrdinalIgnoreCase))
                {
                    args += " " + gopArg;
                }
                else
                {
                    args += " " + keyFrameArg + gopArg;
                }

                // args += " -mixed-refs 0 -refs 3 -x264opts b_pyramid=0:weightb=0:weightp=0";

                var hasGraphicalSubs = state.SubtitleStream != null && !state.SubtitleStream.IsTextSubtitleStream && state.SubtitleDeliveryMethod == SubtitleDeliveryMethod.Encode;

                // This is for graphical subs
                if (hasGraphicalSubs)
                {
                    args += _encodingHelper.GetGraphicalSubtitleParam(state, encodingOptions, codec);
                }

                // Add resolution params, if specified
                else
                {
                    args += _encodingHelper.GetOutputSizeParam(state, encodingOptions, codec);
                }

                // -start_at_zero is necessary to use with -ss when seeking,
                // otherwise the target position cannot be determined.
                if (!(state.SubtitleStream != null && state.SubtitleStream.IsExternal && !state.SubtitleStream.IsTextSubtitleStream))
                {
                    args += " -start_at_zero";
                }

                // args += " -flags -global_header";
            }

            if (!string.IsNullOrEmpty(state.OutputVideoSync))
            {
                args += " -vsync " + state.OutputVideoSync;
            }

            args += _encodingHelper.GetOutputFFlags(state);

            return args;
        }

        private string GetSegmentFileExtension(string? segmentContainer)
        {
            if (!string.IsNullOrWhiteSpace(segmentContainer))
            {
                return "." + segmentContainer;
            }

            return ".ts";
        }

        private string GetSegmentPath(StreamState state, string playlist, int index)
        {
            var folder = Path.GetDirectoryName(playlist);

            var filename = Path.GetFileNameWithoutExtension(playlist);

            return Path.Combine(folder, filename + index.ToString(CultureInfo.InvariantCulture) + GetSegmentFileExtension(state.Request.SegmentContainer));
        }

        private async Task<ActionResult> GetSegmentResult(
            StreamState state,
            string playlistPath,
            string segmentPath,
            string segmentExtension,
            int segmentIndex,
            TranscodingJobDto? transcodingJob,
            CancellationToken cancellationToken)
        {
            var segmentExists = System.IO.File.Exists(segmentPath);
            if (segmentExists)
            {
                if (transcodingJob != null && transcodingJob.HasExited)
                {
                    // Transcoding job is over, so assume all existing files are ready
                    _logger.LogDebug("serving up {0} as transcode is over", segmentPath);
                    return GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob);
                }

                var currentTranscodingIndex = GetCurrentTranscodingIndex(playlistPath, segmentExtension);

                // If requested segment is less than transcoding position, we can't transcode backwards, so assume it's ready
                if (segmentIndex < currentTranscodingIndex)
                {
                    _logger.LogDebug("serving up {0} as transcode index {1} is past requested point {2}", segmentPath, currentTranscodingIndex, segmentIndex);
                    return GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob);
                }
            }

            var nextSegmentPath = GetSegmentPath(state, playlistPath, segmentIndex + 1);
            if (transcodingJob != null)
            {
                while (!cancellationToken.IsCancellationRequested && !transcodingJob.HasExited)
                {
                    // To be considered ready, the segment file has to exist AND
                    // either the transcoding job should be done or next segment should also exist
                    if (segmentExists)
                    {
                        if (transcodingJob.HasExited || System.IO.File.Exists(nextSegmentPath))
                        {
                            _logger.LogDebug("serving up {0} as it deemed ready", segmentPath);
                            return GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob);
                        }
                    }
                    else
                    {
                        segmentExists = System.IO.File.Exists(segmentPath);
                        if (segmentExists)
                        {
                            continue; // avoid unnecessary waiting if segment just became available
                        }
                    }

                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                }

                if (!System.IO.File.Exists(segmentPath))
                {
                    _logger.LogWarning("cannot serve {0} as transcoding quit before we got there", segmentPath);
                }
                else
                {
                    _logger.LogDebug("serving {0} as it's on disk and transcoding stopped", segmentPath);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
            else
            {
                _logger.LogWarning("cannot serve {0} as it doesn't exist and no transcode is running", segmentPath);
            }

            return GetSegmentResult(state, segmentPath, segmentIndex, transcodingJob);
        }

        private ActionResult GetSegmentResult(StreamState state, string segmentPath, int index, TranscodingJobDto? transcodingJob)
        {
            var segmentEndingPositionTicks = GetEndPositionTicks(state, index);

            Response.OnCompleted(() =>
            {
                _logger.LogDebug("finished serving {0}", segmentPath);
                if (transcodingJob != null)
                {
                    transcodingJob.DownloadPositionTicks = Math.Max(transcodingJob.DownloadPositionTicks ?? segmentEndingPositionTicks, segmentEndingPositionTicks);
                    _transcodingJobHelper.OnTranscodeEndRequest(transcodingJob);
                }

                return Task.CompletedTask;
            });

            return FileStreamResponseHelpers.GetStaticFileResult(segmentPath, MimeTypes.GetMimeType(segmentPath)!, false, HttpContext);
        }

        private long GetEndPositionTicks(StreamState state, int requestedIndex)
        {
            double startSeconds = 0;
            var lengths = GetSegmentLengths(state);

            if (requestedIndex >= lengths.Length)
            {
                var msg = string.Format(
                    CultureInfo.InvariantCulture,
                    "Invalid segment index requested: {0} - Segment count: {1}",
                    requestedIndex,
                    lengths.Length);
                throw new ArgumentException(msg);
            }

            for (var i = 0; i <= requestedIndex; i++)
            {
                startSeconds += lengths[i];
            }

            return TimeSpan.FromSeconds(startSeconds).Ticks;
        }

        private int? GetCurrentTranscodingIndex(string playlist, string segmentExtension)
        {
            var job = _transcodingJobHelper.GetTranscodingJob(playlist, _transcodingJobType);

            if (job == null || job.HasExited)
            {
                return null;
            }

            var file = GetLastTranscodingFile(playlist, segmentExtension, _fileSystem);

            if (file == null)
            {
                return null;
            }

            var playlistFilename = Path.GetFileNameWithoutExtension(playlist);

            var indexString = Path.GetFileNameWithoutExtension(file.Name).Substring(playlistFilename.Length);

            return int.Parse(indexString, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        private static FileSystemMetadata? GetLastTranscodingFile(string playlist, string segmentExtension, IFileSystem fileSystem)
        {
            var folder = Path.GetDirectoryName(playlist);

            var filePrefix = Path.GetFileNameWithoutExtension(playlist) ?? string.Empty;

            try
            {
                return fileSystem.GetFiles(folder, new[] { segmentExtension }, true, false)
                    .Where(i => Path.GetFileNameWithoutExtension(i.Name).StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(fileSystem.GetLastWriteTimeUtc)
                    .FirstOrDefault();
            }
            catch (IOException)
            {
                return null;
            }
        }

        private void DeleteLastFile(string playlistPath, string segmentExtension, int retryCount)
        {
            var file = GetLastTranscodingFile(playlistPath, segmentExtension, _fileSystem);

            if (file != null)
            {
                DeleteFile(file.FullName, retryCount);
            }
        }

        private void DeleteFile(string path, int retryCount)
        {
            if (retryCount >= 5)
            {
                return;
            }

            _logger.LogDebug("Deleting partial HLS file {path}", path);

            try
            {
                _fileSystem.DeleteFile(path);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error deleting partial stream file(s) {path}", path);

                var task = Task.Delay(100);
                Task.WaitAll(task);
                DeleteFile(path, retryCount + 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting partial stream file(s) {path}", path);
            }
        }

        private long GetStartPositionTicks(StreamState state, int requestedIndex)
        {
            double startSeconds = 0;
            var lengths = GetSegmentLengths(state);

            if (requestedIndex >= lengths.Length)
            {
                var msg = string.Format(
                    CultureInfo.InvariantCulture,
                    "Invalid segment index requested: {0} - Segment count: {1}",
                    requestedIndex,
                    lengths.Length);
                throw new ArgumentException(msg);
            }

            for (var i = 0; i < requestedIndex; i++)
            {
                startSeconds += lengths[i];
            }

            var position = TimeSpan.FromSeconds(startSeconds).Ticks;
            return position;
        }
    }
}
