using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Api.Models.StreamingDtos;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The universal audio controller.
    /// </summary>
    [Route("")]
    public class UniversalAudioController : BaseJellyfinApiController
    {
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<UniversalAudioController> _logger;
        private readonly MediaInfoHelper _mediaInfoHelper;
        private readonly AudioHelper _audioHelper;
        private readonly DynamicHlsHelper _dynamicHlsHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniversalAudioController"/> class.
        /// </summary>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{UniversalAudioController}"/> interface.</param>
        /// <param name="mediaInfoHelper">Instance of <see cref="MediaInfoHelper"/>.</param>
        /// <param name="audioHelper">Instance of <see cref="AudioHelper"/>.</param>
        /// <param name="dynamicHlsHelper">Instance of <see cref="DynamicHlsHelper"/>.</param>
        public UniversalAudioController(
            ILibraryManager libraryManager,
            ILogger<UniversalAudioController> logger,
            MediaInfoHelper mediaInfoHelper,
            AudioHelper audioHelper,
            DynamicHlsHelper dynamicHlsHelper)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _mediaInfoHelper = mediaInfoHelper;
            _audioHelper = audioHelper;
            _dynamicHlsHelper = dynamicHlsHelper;
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
        /// <param name="audioBitRate">Optional. Specify an audio bitrate to encode to, e.g. 128000. If omitted this will be left to encoder defaults.</param>
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
        [HttpGet("Audio/{itemId}/universal")]
        [HttpHead("Audio/{itemId}/universal", Name = "HeadUniversalAudioStream")]
        [Authorize(Policy = Policies.DefaultAuthorization)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesAudioFile]
        public async Task<ActionResult> GetUniversalAudioStream(
            [FromRoute, Required] Guid itemId,
            [FromQuery, ModelBinder(typeof(CommaDelimitedArrayModelBinder))] string[] container,
            [FromQuery] string? mediaSourceId,
            [FromQuery] string? deviceId,
            [FromQuery] Guid? userId,
            [FromQuery] string? audioCodec,
            [FromQuery] int? maxAudioChannels,
            [FromQuery] int? transcodingAudioChannels,
            [FromQuery] int? maxStreamingBitrate,
            [FromQuery] int? audioBitRate,
            [FromQuery] long? startTimeTicks,
            [FromQuery] string? transcodingContainer,
            [FromQuery] string? transcodingProtocol,
            [FromQuery] int? maxAudioSampleRate,
            [FromQuery] int? maxAudioBitDepth,
            [FromQuery] bool? enableRemoteMedia,
            [FromQuery] bool breakOnNonKeyFrames = false,
            [FromQuery] bool enableRedirection = true)
        {
            var deviceProfile = GetDeviceProfile(container, transcodingContainer, audioCodec, transcodingProtocol, breakOnNonKeyFrames, transcodingAudioChannels, maxAudioSampleRate, maxAudioBitDepth, maxAudioChannels);

            if (!userId.HasValue || userId.Value.Equals(default))
            {
                userId = User.GetUserId();
            }

            _logger.LogInformation("GetPostedPlaybackInfo profile: {@Profile}", deviceProfile);

            var info = await _mediaInfoHelper.GetPlaybackInfo(
                    itemId,
                    userId,
                    mediaSourceId)
                .ConfigureAwait(false);

            // set device specific data
            var item = _libraryManager.GetItemById(itemId);

            foreach (var sourceInfo in info.MediaSources)
            {
                _mediaInfoHelper.SetDeviceSpecificData(
                    item,
                    sourceInfo,
                    deviceProfile,
                    User,
                    maxStreamingBitrate ?? deviceProfile.MaxStreamingBitrate,
                    startTimeTicks ?? 0,
                    mediaSourceId ?? string.Empty,
                    null,
                    null,
                    maxAudioChannels,
                    info.PlaySessionId!,
                    userId ?? Guid.Empty,
                    true,
                    true,
                    true,
                    true,
                    true,
                    Request.HttpContext.GetNormalizedRemoteIp());
            }

            _mediaInfoHelper.SortMediaSources(info, maxStreamingBitrate);

            foreach (var source in info.MediaSources)
            {
                _mediaInfoHelper.NormalizeMediaSourceContainer(source, deviceProfile, DlnaProfileType.Video);
            }

            var mediaSource = info.MediaSources[0];
            if (mediaSource.SupportsDirectPlay && mediaSource.Protocol == MediaProtocol.Http && enableRedirection && mediaSource.IsRemote && enableRemoteMedia.HasValue && enableRemoteMedia.Value)
            {
                return Redirect(mediaSource.Path);
            }

            var isStatic = mediaSource.SupportsDirectStream;
            if (!isStatic && string.Equals(mediaSource.TranscodingSubProtocol, "hls", StringComparison.OrdinalIgnoreCase))
            {
                // hls segment container can only be mpegts or fmp4 per ffmpeg documentation
                // ffmpeg option -> file extension
                //        mpegts -> ts
                //          fmp4 -> mp4
                // TODO: remove this when we switch back to the segment muxer
                var supportedHlsContainers = new[] { "ts", "mp4" };

                var dynamicHlsRequestDto = new HlsAudioRequestDto
                {
                    Id = itemId,
                    Container = ".m3u8",
                    Static = isStatic,
                    PlaySessionId = info.PlaySessionId,
                    // fallback to mpegts if device reports some weird value unsupported by hls
                    SegmentContainer = Array.Exists(supportedHlsContainers, element => element == transcodingContainer) ? transcodingContainer : "ts",
                    MediaSourceId = mediaSourceId,
                    DeviceId = deviceId,
                    AudioCodec = audioCodec,
                    EnableAutoStreamCopy = true,
                    AllowAudioStreamCopy = true,
                    AllowVideoStreamCopy = true,
                    BreakOnNonKeyFrames = breakOnNonKeyFrames,
                    AudioSampleRate = maxAudioSampleRate,
                    MaxAudioChannels = maxAudioChannels,
                    MaxAudioBitDepth = maxAudioBitDepth,
                    AudioBitRate = audioBitRate ?? maxStreamingBitrate,
                    StartTimeTicks = startTimeTicks,
                    SubtitleMethod = SubtitleDeliveryMethod.Hls,
                    RequireAvc = false,
                    DeInterlace = false,
                    RequireNonAnamorphic = false,
                    EnableMpegtsM2TsMode = false,
                    TranscodeReasons = mediaSource.TranscodeReasons == 0 ? null : mediaSource.TranscodeReasons.ToString(),
                    Context = EncodingContext.Static,
                    StreamOptions = new Dictionary<string, string>(),
                    EnableAdaptiveBitrateStreaming = true
                };

                return await _dynamicHlsHelper.GetMasterHlsPlaylist(TranscodingJobType.Hls, dynamicHlsRequestDto, true)
                    .ConfigureAwait(false);
            }

            var audioStreamingDto = new StreamingRequestDto
            {
                Id = itemId,
                Container = isStatic ? null : ("." + mediaSource.TranscodingContainer),
                Static = isStatic,
                PlaySessionId = info.PlaySessionId,
                MediaSourceId = mediaSourceId,
                DeviceId = deviceId,
                AudioCodec = audioCodec,
                EnableAutoStreamCopy = true,
                AllowAudioStreamCopy = true,
                AllowVideoStreamCopy = true,
                BreakOnNonKeyFrames = breakOnNonKeyFrames,
                AudioSampleRate = maxAudioSampleRate,
                MaxAudioChannels = maxAudioChannels,
                AudioBitRate = isStatic ? null : (audioBitRate ?? maxStreamingBitrate),
                MaxAudioBitDepth = maxAudioBitDepth,
                AudioChannels = maxAudioChannels,
                CopyTimestamps = true,
                StartTimeTicks = startTimeTicks,
                SubtitleMethod = SubtitleDeliveryMethod.Embed,
                TranscodeReasons = mediaSource.TranscodeReasons == 0 ? null : mediaSource.TranscodeReasons.ToString(),
                Context = EncodingContext.Static
            };

            return await _audioHelper.GetAudioStream(TranscodingJobType.Progressive, audioStreamingDto).ConfigureAwait(false);
        }

        private DeviceProfile GetDeviceProfile(
            string[] containers,
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

            int len = containers.Length;
            var directPlayProfiles = new DirectPlayProfile[len];
            for (int i = 0; i < len; i++)
            {
                var parts = containers[i].Split('|', StringSplitOptions.RemoveEmptyEntries);

                var audioCodecs = parts.Length == 1 ? null : string.Join(',', parts.Skip(1));

                directPlayProfiles[i] = new DirectPlayProfile
                {
                    Type = DlnaProfileType.Audio,
                    Container = parts[0],
                    AudioCodec = audioCodecs
                };
            }

            deviceProfile.DirectPlayProfiles = directPlayProfiles;

            deviceProfile.TranscodingProfiles = new[]
            {
                new TranscodingProfile
                {
                    Type = DlnaProfileType.Audio,
                    Context = EncodingContext.Streaming,
                    Container = transcodingContainer ?? "mp3",
                    AudioCodec = audioCodec ?? "mp3",
                    Protocol = transcodingProtocol ?? "http",
                    BreakOnNonKeyFrames = breakOnNonKeyFrames ?? false,
                    MaxAudioChannels = transcodingAudioChannels?.ToString(CultureInfo.InvariantCulture)
                }
            };

            var codecProfiles = new List<CodecProfile>();
            var conditions = new List<ProfileCondition>();

            if (maxAudioSampleRate.HasValue)
            {
                // codec profile
                conditions.Add(
                    new ProfileCondition
                    {
                        Condition = ProfileConditionType.LessThanEqual,
                        IsRequired = false,
                        Property = ProfileConditionValue.AudioSampleRate,
                        Value = maxAudioSampleRate.Value.ToString(CultureInfo.InvariantCulture)
                    });
            }

            if (maxAudioBitDepth.HasValue)
            {
                // codec profile
                conditions.Add(
                    new ProfileCondition
                    {
                        Condition = ProfileConditionType.LessThanEqual,
                        IsRequired = false,
                        Property = ProfileConditionValue.AudioBitDepth,
                        Value = maxAudioBitDepth.Value.ToString(CultureInfo.InvariantCulture)
                    });
            }

            if (maxAudioChannels.HasValue)
            {
                // codec profile
                conditions.Add(
                    new ProfileCondition
                    {
                        Condition = ProfileConditionType.LessThanEqual,
                        IsRequired = false,
                        Property = ProfileConditionValue.AudioChannels,
                        Value = maxAudioChannels.Value.ToString(CultureInfo.InvariantCulture)
                    });
            }

            if (conditions.Count > 0)
            {
                // codec profile
                codecProfiles.Add(
                    new CodecProfile
                    {
                        Type = CodecType.Audio,
                        Container = string.Join(',', containers),
                        Conditions = conditions.ToArray()
                    });
            }

            deviceProfile.CodecProfiles = codecProfiles.ToArray();

            return deviceProfile;
        }
    }
}
