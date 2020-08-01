using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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
        private readonly IStreamHelper _streamHelper;

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
            IStreamHelper streamHelper)
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
            _streamHelper = streamHelper;
        }

        [HttpGet("/Audio/{itemId}/universal")]
        [HttpGet("/Audio/{itemId}/{universal=universal}.{container?}")]
        [HttpHead("/Audio/{itemId}/universal")]
        [HttpHead("/Audio/{itemId}/{universal=universal}.{container?}")]
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
                // TODO new DynamicHlsController
                // var dynamicHlsController = new DynamicHlsController();
                var transcodingProfile = deviceProfile.TranscodingProfiles[0];

                // hls segment container can only be mpegts or fmp4 per ffmpeg documentation
                // TODO: remove this when we switch back to the segment muxer
                var supportedHLSContainers = new[] { "mpegts", "fmp4" };

                /*
                var newRequest = new GetMasterHlsAudioPlaylist
                {
                    AudioBitRate = isStatic ? (int?)null : Convert.ToInt32(Math.Min(request.MaxStreamingBitrate ?? 192000, int.MaxValue)),
                    AudioCodec = transcodingProfile.AudioCodec,
                    Container = ".m3u8",
                    DeviceId = request.DeviceId,
                    Id = request.Id,
                    MaxAudioChannels = request.MaxAudioChannels,
                    MediaSourceId = mediaSource.Id,
                    PlaySessionId = playbackInfoResult.PlaySessionId,
                    StartTimeTicks = request.StartTimeTicks,
                    Static = isStatic,
                    // fallback to mpegts if device reports some weird value unsupported by hls
                    SegmentContainer = Array.Exists(supportedHLSContainers, element => element == request.TranscodingContainer) ? request.TranscodingContainer : "mpegts",
                    AudioSampleRate = request.MaxAudioSampleRate,
                    MaxAudioBitDepth = request.MaxAudioBitDepth,
                    BreakOnNonKeyFrames = transcodingProfile.BreakOnNonKeyFrames,
                    TranscodeReasons = mediaSource.TranscodeReasons == null ? null : string.Join(",", mediaSource.TranscodeReasons.Select(i => i.ToString()).ToArray())
                };

                if (isHeadRequest)
                {
                    audioController.Request.Method = HttpMethod.Head.Method;
                    return await service.Head(newRequest).ConfigureAwait(false);
                }

                return await service.Get(newRequest).ConfigureAwait(false);*/
                // TODO remove this line
                return Content(string.Empty);
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
                    _streamHelper,
                    _fileSystem,
                    _subtitleEncoder,
                    _configuration,
                    _deviceManager,
                    _transcodingJobHelper,
                    // TODO HttpClient
                    new HttpClient());

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

            var containers = (container ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var cont in containers)
            {
                var parts = cont.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

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
