using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The universal audio controller.
    /// </summary>
    public class UniversalAudioController : BaseJellyfinApiController
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IDeviceManager _deviceManager;
        private readonly IDlnaManager _dlnaManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IFileSystem _fileSystem;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IAuthorizationContext _authorizationContext;
        private readonly INetworkManager _networkManager;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly TranscodingJobHelper _transcodingJobHelper;
        private readonly IConfiguration _configuration;
        private readonly ISubtitleEncoder _subtitleEncoder;
        private readonly IHttpClientFactory _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniversalAudioController"/> class.
        /// </summary>
        /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        /// <param name="dlnaManager">Instance of the <see cref="IDlnaManager"/> interface.</param>
        /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="authorizationContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="transcodingJobHelper">Instance of the <see cref="TranscodingJobHelper"/> interface.</param>
        /// <param name="configuration">Instance of the <see cref="IConfiguration"/> interface.</param>
        /// <param name="subtitleEncoder">Instance of the <see cref="ISubtitleEncoder"/> interface.</param>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        public UniversalAudioController(
            ILoggerFactory loggerFactory,
            IServerConfigurationManager serverConfigurationManager,
            IUserManager userManager,
            ILibraryManager libraryManager,
            IMediaEncoder mediaEncoder,
            IFileSystem fileSystem,
            IDlnaManager dlnaManager,
            IDeviceManager deviceManager,
            IMediaSourceManager mediaSourceManager,
            IAuthorizationContext authorizationContext,
            INetworkManager networkManager,
            TranscodingJobHelper transcodingJobHelper,
            IConfiguration configuration,
            ISubtitleEncoder subtitleEncoder,
            IHttpClientFactory httpClientFactory)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _mediaEncoder = mediaEncoder;
            _fileSystem = fileSystem;
            _dlnaManager = dlnaManager;
            _deviceManager = deviceManager;
            _mediaSourceManager = mediaSourceManager;
            _authorizationContext = authorizationContext;
            _networkManager = networkManager;
            _loggerFactory = loggerFactory;
            _serverConfigurationManager = serverConfigurationManager;
            _transcodingJobHelper = transcodingJobHelper;
            _configuration = configuration;
            _subtitleEncoder = subtitleEncoder;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Gets an audio stream.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="container">Optional. The audio container.</param>
        /// <param name="mediaSourceId">The media version id, if playing an alternate version.</param>
        /// <param name="deviceId">The device id of the client requesting. Used to stop encoding processes when needed.</param>
        /// <param name="userId">Optional. The user id.</param>
        /// <param name="audioCodec">Optional. The audio codec to transcode to.</param>
        /// <param name="maxAudioChannels">Optional. The maximum number of audio channels.</param>
        /// <param name="transcodingAudioChannels">Optional. The number of how many audio channels to transcode to.</param>
        /// <param name="maxStreamingBitrate">Optional. The maximum streaming bitrate.</param>
        /// <param name="startTimeTicks">Optional. Specify a starting offset, in ticks. 1 tick = 10000 ms.</param>
        /// <param name="transcodingContainer">Optional. The container to transcode to.</param>
        /// <param name="transcodingProtocol">Optional. The transcoding protocol.</param>
        /// <param name="maxAudioSampleRate">Optional. The maximum audio sample rate.</param>
        /// <param name="maxAudioBitDepth">Optional. The maximum audio bit depth.</param>
        /// <param name="enableRemoteMedia">Optional. Whether to enable remote media.</param>
        /// <param name="breakOnNonKeyFrames">Optional. Whether to break on non key frames.</param>
        /// <param name="enableRedirection">Whether to enable redirection. Defaults to true.</param>
        /// <response code="200">Audio stream returned.</response>
        /// <response code="302">Redirected to remote audio stream.</response>
        /// <returns>A <see cref="Task"/> containing the audio file.</returns>
        [HttpGet("/Audio/{itemId}/universal")]
        [HttpGet("/Audio/{itemId}/{universal=universal}.{container?}")]
        [HttpHead("/Audio/{itemId}/universal")]
        [HttpHead("/Audio/{itemId}/{universal=universal}.{container?}")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status302Found)]
        public async Task<ActionResult> GetUniversalAudioStream(
            [FromRoute] Guid itemId,
            [FromRoute] string? container,
            [FromQuery] string? mediaSourceId,
            [FromQuery] string? deviceId,
            [FromQuery] Guid? userId,
            [FromQuery] string? audioCodec,
            [FromQuery] int? maxAudioChannels,
            [FromQuery] int? transcodingAudioChannels,
            [FromQuery] long? maxStreamingBitrate,
            [FromQuery] long? startTimeTicks,
            [FromQuery] string? transcodingContainer,
            [FromQuery] string? transcodingProtocol,
            [FromQuery] int? maxAudioSampleRate,
            [FromQuery] int? maxAudioBitDepth,
            [FromQuery] bool? enableRemoteMedia,
            [FromQuery] bool breakOnNonKeyFrames,
            [FromQuery] bool enableRedirection = true)
        {
            bool isHeadRequest = Request.Method == System.Net.WebRequestMethods.Http.Head;
            var deviceProfile = GetDeviceProfile(container, transcodingContainer, audioCodec, transcodingProtocol, breakOnNonKeyFrames, transcodingAudioChannels, maxAudioSampleRate, maxAudioBitDepth, maxAudioChannels);
            _authorizationContext.GetAuthorizationInfo(Request).DeviceId = deviceId;

            var mediaInfoController = new MediaInfoController(_mediaSourceManager, _deviceManager, _libraryManager, _networkManager, _mediaEncoder, _userManager, _authorizationContext, _loggerFactory.CreateLogger<MediaInfoController>(), _serverConfigurationManager);
            var playbackInfoResult = await mediaInfoController.GetPlaybackInfo(itemId, userId).ConfigureAwait(false);
            var mediaSource = playbackInfoResult.Value.MediaSources[0];

            if (mediaSource.SupportsDirectPlay && mediaSource.Protocol == MediaProtocol.Http)
            {
                if (enableRedirection)
                {
                    if (mediaSource.IsRemote && enableRemoteMedia.HasValue && enableRemoteMedia.Value)
                    {
                        return Redirect(mediaSource.Path);
                    }
                }
            }

            var isStatic = mediaSource.SupportsDirectStream;
            if (!isStatic && string.Equals(mediaSource.TranscodingSubProtocol, "hls", StringComparison.OrdinalIgnoreCase))
            {
                var dynamicHlsController = new DynamicHlsController(
                    _libraryManager,
                    _userManager,
                    _dlnaManager,
                    _authorizationContext,
                    _mediaSourceManager,
                    _serverConfigurationManager,
                    _mediaEncoder,
                    _fileSystem,
                    _subtitleEncoder,
                    _configuration,
                    _deviceManager,
                    _transcodingJobHelper,
                    _networkManager,
                    _loggerFactory.CreateLogger<DynamicHlsController>());
                var transcodingProfile = deviceProfile.TranscodingProfiles[0];

                // hls segment container can only be mpegts or fmp4 per ffmpeg documentation
                // TODO: remove this when we switch back to the segment muxer
                var supportedHlsContainers = new[] { "mpegts", "fmp4" };

                if (isHeadRequest)
                {
                    dynamicHlsController.Request.Method = HttpMethod.Head.Method;
                    return await dynamicHlsController.GetMasterHlsAudioPlaylist(
                        itemId,
                        ".m3u8",
                        isStatic,
                        null,
                        null,
                        null,
                        playbackInfoResult.Value.PlaySessionId,
                        // fallback to mpegts if device reports some weird value unsupported by hls
                        Array.Exists(supportedHlsContainers, element => element == transcodingContainer) ? transcodingContainer : "mpegts",
                        null,
                        null,
                        mediaSource.Id,
                        deviceId,
                        transcodingProfile.AudioCodec,
                        null,
                        null,
                        null,
                        transcodingProfile.BreakOnNonKeyFrames,
                        maxAudioSampleRate,
                        maxAudioBitDepth,
                        null,
                        isStatic ? (int?)null : Convert.ToInt32(Math.Min(maxStreamingBitrate ?? 192000, int.MaxValue)),
                        maxAudioChannels,
                        null,
                        null,
                        null,
                        null,
                        null,
                        startTimeTicks,
                        null,
                        null,
                        null,
                        null,
                        SubtitleDeliveryMethod.Hls,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        mediaSource.TranscodeReasons == null ? null : string.Join(",", mediaSource.TranscodeReasons.Select(i => i.ToString()).ToArray()),
                        null,
                        null,
                        EncodingContext.Static,
                        new Dictionary<string, string>())
                        .ConfigureAwait(false);
                }

                return await dynamicHlsController.GetMasterHlsAudioPlaylist(
                    itemId,
                    ".m3u8",
                    isStatic,
                    null,
                    null,
                    null,
                    playbackInfoResult.Value.PlaySessionId,
                    // fallback to mpegts if device reports some weird value unsupported by hls
                    Array.Exists(supportedHlsContainers, element => element == transcodingContainer) ? transcodingContainer : "mpegts",
                    null,
                    null,
                    mediaSource.Id,
                    deviceId,
                    transcodingProfile.AudioCodec,
                    null,
                    null,
                    null,
                    transcodingProfile.BreakOnNonKeyFrames,
                    maxAudioSampleRate,
                    maxAudioBitDepth,
                    null,
                    isStatic ? (int?)null : Convert.ToInt32(Math.Min(maxStreamingBitrate ?? 192000, int.MaxValue)),
                    maxAudioChannels,
                    null,
                    null,
                    null,
                    null,
                    null,
                    startTimeTicks,
                    null,
                    null,
                    null,
                    null,
                    SubtitleDeliveryMethod.Hls,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    mediaSource.TranscodeReasons == null ? null : string.Join(",", mediaSource.TranscodeReasons.Select(i => i.ToString()).ToArray()),
                    null,
                    null,
                    EncodingContext.Static,
                    new Dictionary<string, string>())
                    .ConfigureAwait(false);
            }
            else
            {
                var audioController = new AudioController(
                    _dlnaManager,
                    _userManager,
                    _authorizationContext,
                    _libraryManager,
                    _mediaSourceManager,
                    _serverConfigurationManager,
                    _mediaEncoder,
                    _fileSystem,
                    _subtitleEncoder,
                    _configuration,
                    _deviceManager,
                    _transcodingJobHelper,
                    _httpClientFactory);

                if (isHeadRequest)
                {
                    audioController.Request.Method = HttpMethod.Head.Method;
                    return await audioController.GetAudioStream(
                            itemId,
                            isStatic ? null : ("." + mediaSource.TranscodingContainer),
                            isStatic,
                            null,
                            null,
                            null,
                            playbackInfoResult.Value.PlaySessionId,
                            null,
                            null,
                            null,
                            mediaSource.Id,
                            deviceId,
                            audioCodec,
                            null,
                            null,
                            null,
                            breakOnNonKeyFrames,
                            maxAudioSampleRate,
                            maxAudioBitDepth,
                            isStatic ? (int?)null : Convert.ToInt32(Math.Min(maxStreamingBitrate ?? 192000, int.MaxValue)),
                            null,
                            maxAudioChannels,
                            null,
                            null,
                            null,
                            null,
                            null,
                            startTimeTicks,
                            null,
                            null,
                            null,
                            null,
                            SubtitleDeliveryMethod.Embed,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            null,
                            mediaSource.TranscodeReasons == null ? null : string.Join(",", mediaSource.TranscodeReasons.Select(i => i.ToString()).ToArray()),
                            null,
                            null,
                            null,
                            null)
                        .ConfigureAwait(false);
                }

                return await audioController.GetAudioStream(
                        itemId,
                        isStatic ? null : ("." + mediaSource.TranscodingContainer),
                        isStatic,
                        null,
                        null,
                        null,
                        playbackInfoResult.Value.PlaySessionId,
                        null,
                        null,
                        null,
                        mediaSource.Id,
                        deviceId,
                        audioCodec,
                        null,
                        null,
                        null,
                        breakOnNonKeyFrames,
                        maxAudioSampleRate,
                        maxAudioBitDepth,
                        isStatic ? (int?)null : Convert.ToInt32(Math.Min(maxStreamingBitrate ?? 192000, int.MaxValue)),
                        null,
                        maxAudioChannels,
                        null,
                        null,
                        null,
                        null,
                        null,
                        startTimeTicks,
                        null,
                        null,
                        null,
                        null,
                        SubtitleDeliveryMethod.Embed,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        mediaSource.TranscodeReasons == null ? null : string.Join(",", mediaSource.TranscodeReasons.Select(i => i.ToString()).ToArray()),
                        null,
                        null,
                        null,
                        null)
                    .ConfigureAwait(false);
            }
        }

        private DeviceProfile GetDeviceProfile(
            string? container,
            string? transcodingContainer,
            string? audioCodec,
            string? transcodingProtocol,
            bool? breakOnNonKeyFrames,
            int? transcodingAudioChannels,
            int? maxAudioSampleRate,
            int? maxAudioBitDepth,
            int? maxAudioChannels)
        {
            var deviceProfile = new DeviceProfile();

            var directPlayProfiles = new List<DirectPlayProfile>();

            var containers = RequestHelpers.Split(container, ',', true);

            foreach (var cont in containers)
            {
                var parts = RequestHelpers.Split(cont, ',', true);

                var audioCodecs = parts.Length == 1 ? null : string.Join(",", parts.Skip(1).ToArray());

                directPlayProfiles.Add(new DirectPlayProfile { Type = DlnaProfileType.Audio, Container = parts[0], AudioCodec = audioCodecs });
            }

            deviceProfile.DirectPlayProfiles = directPlayProfiles.ToArray();

            deviceProfile.TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Type = DlnaProfileType.Audio,
                    Context = EncodingContext.Streaming,
                    Container = transcodingContainer,
                    AudioCodec = audioCodec,
                    Protocol = transcodingProtocol,
                    BreakOnNonKeyFrames = breakOnNonKeyFrames ?? false,
                    MaxAudioChannels = transcodingAudioChannels?.ToString(CultureInfo.InvariantCulture)
                }
            };

            var codecProfiles = new List<CodecProfile>();
            var conditions = new List<ProfileCondition>();

            if (maxAudioSampleRate.HasValue)
            {
                // codec profile
                conditions.Add(new ProfileCondition { Condition = ProfileConditionType.LessThanEqual, IsRequired = false, Property = ProfileConditionValue.AudioSampleRate, Value = maxAudioSampleRate.Value.ToString(CultureInfo.InvariantCulture) });
            }

            if (maxAudioBitDepth.HasValue)
            {
                // codec profile
                conditions.Add(new ProfileCondition { Condition = ProfileConditionType.LessThanEqual, IsRequired = false, Property = ProfileConditionValue.AudioBitDepth, Value = maxAudioBitDepth.Value.ToString(CultureInfo.InvariantCulture) });
            }

            if (maxAudioChannels.HasValue)
            {
                // codec profile
                conditions.Add(new ProfileCondition { Condition = ProfileConditionType.LessThanEqual, IsRequired = false, Property = ProfileConditionValue.AudioChannels, Value = maxAudioChannels.Value.ToString(CultureInfo.InvariantCulture) });
            }

            if (conditions.Count > 0)
            {
                // codec profile
                codecProfiles.Add(new CodecProfile { Type = CodecType.Audio, Container = container, Conditions = conditions.ToArray() });
            }

            deviceProfile.CodecProfiles = codecProfiles.ToArray();

            return deviceProfile;
        }
    }
}
