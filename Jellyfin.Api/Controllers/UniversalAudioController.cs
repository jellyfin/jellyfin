using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.Models.VideoDtos;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The universal audio controller.
    /// </summary>
    public class UniversalAudioController : BaseJellyfinApiController
    {
        private readonly IAuthorizationContext _authorizationContext;
        private readonly MediaInfoController _mediaInfoController;
        private readonly DynamicHlsController _dynamicHlsController;
        private readonly AudioController _audioController;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniversalAudioController"/> class.
        /// </summary>
        /// <param name="authorizationContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="mediaInfoController">Instance of the <see cref="MediaInfoController"/>.</param>
        /// <param name="dynamicHlsController">Instance of the <see cref="DynamicHlsController"/>.</param>
        /// <param name="audioController">Instance of the <see cref="AudioController"/>.</param>
        public UniversalAudioController(
            IAuthorizationContext authorizationContext,
            MediaInfoController mediaInfoController,
            DynamicHlsController dynamicHlsController,
            AudioController audioController)
        {
            _authorizationContext = authorizationContext;
            _mediaInfoController = mediaInfoController;
            _dynamicHlsController = dynamicHlsController;
            _audioController = audioController;
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
        [HttpGet("/Audio/{itemId}/{universal=universal}.{container?}", Name = "GetUniversalAudioStream_2")]
        [HttpHead("/Audio/{itemId}/universal", Name = "HeadUniversalAudioStream")]
        [HttpHead("/Audio/{itemId}/{universal=universal}.{container?}", Name = "HeadUniversalAudioStream_2")]
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

            var playbackInfoResult = await _mediaInfoController.GetPostedPlaybackInfo(
                itemId,
                userId,
                maxStreamingBitrate,
                startTimeTicks,
                null,
                null,
                maxAudioChannels,
                mediaSourceId,
                null,
                new DeviceProfileDto { DeviceProfile = deviceProfile })
                .ConfigureAwait(false);
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
                var transcodingProfile = deviceProfile.TranscodingProfiles[0];

                // hls segment container can only be mpegts or fmp4 per ffmpeg documentation
                // TODO: remove this when we switch back to the segment muxer
                var supportedHlsContainers = new[] { "mpegts", "fmp4" };

                if (isHeadRequest)
                {
                    _dynamicHlsController.Request.Method = HttpMethod.Head.Method;
                }

                return await _dynamicHlsController.GetMasterHlsAudioPlaylist(
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
                if (isHeadRequest)
                {
                    _audioController.Request.Method = HttpMethod.Head.Method;
                }

                return await _audioController.GetAudioStream(
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
