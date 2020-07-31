using System;
using System.Buffers;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Models.VideoDtos;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// The media info controller.
    /// </summary>
    [Authorize(Policy = Policies.DefaultAuthorization)]
    public class MediaInfoController : BaseJellyfinApiController
    {
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IDeviceManager _deviceManager;
        private readonly ILibraryManager _libraryManager;
        private readonly INetworkManager _networkManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IUserManager _userManager;
        private readonly IAuthorizationContext _authContext;
        private readonly ILogger _logger;
        private readonly IServerConfigurationManager _serverConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfoController"/> class.
        /// </summary>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{MediaInfoController}"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public MediaInfoController(
            IMediaSourceManager mediaSourceManager,
            IDeviceManager deviceManager,
            ILibraryManager libraryManager,
            INetworkManager networkManager,
            IMediaEncoder mediaEncoder,
            IUserManager userManager,
            IAuthorizationContext authContext,
            ILogger<MediaInfoController> logger,
            IServerConfigurationManager serverConfigurationManager)
        {
            _mediaSourceManager = mediaSourceManager;
            _deviceManager = deviceManager;
            _libraryManager = libraryManager;
            _networkManager = networkManager;
            _mediaEncoder = mediaEncoder;
            _userManager = userManager;
            _authContext = authContext;
            _logger = logger;
            _serverConfigurationManager = serverConfigurationManager;
        }

        /// <summary>
        /// Gets live playback media info for an item.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <response code="200">Playback info returned.</response>
        /// <returns>A <see cref="Task"/> containing a <see cref="PlaybackInfoResponse"/> with the playback information.</returns>
        [HttpGet("/Items/{itemId}/PlaybackInfo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PlaybackInfoResponse>> GetPlaybackInfo([FromRoute] Guid itemId, [FromQuery] Guid? userId)
        {
            return await GetPlaybackInfoInternal(itemId, userId, null, null).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets live playback media info for an item.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="maxStreamingBitrate">The maximum streaming bitrate.</param>
        /// <param name="startTimeTicks">The start time in ticks.</param>
        /// <param name="audioStreamIndex">The audio stream index.</param>
        /// <param name="subtitleStreamIndex">The subtitle stream index.</param>
        /// <param name="maxAudioChannels">The maximum number of audio channels.</param>
        /// <param name="mediaSourceId">The media source id.</param>
        /// <param name="liveStreamId">The livestream id.</param>
        /// <param name="deviceProfile">The device profile.</param>
        /// <param name="autoOpenLiveStream">Whether to auto open the livestream.</param>
        /// <param name="enableDirectPlay">Whether to enable direct play. Default: true.</param>
        /// <param name="enableDirectStream">Whether to enable direct stream. Default: true.</param>
        /// <param name="enableTranscoding">Whether to enable transcoding. Default: true.</param>
        /// <param name="allowVideoStreamCopy">Whether to allow to copy the video stream. Default: true.</param>
        /// <param name="allowAudioStreamCopy">Whether to allow to copy the audio stream. Default: true.</param>
        /// <response code="200">Playback info returned.</response>
        /// <returns>A <see cref="Task"/> containing a <see cref="PlaybackInfoResponse"/> with the playback info.</returns>
        [HttpPost("/Items/{itemId}/PlaybackInfo")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PlaybackInfoResponse>> GetPostedPlaybackInfo(
            [FromRoute] Guid itemId,
            [FromQuery] Guid? userId,
            [FromQuery] long? maxStreamingBitrate,
            [FromQuery] long? startTimeTicks,
            [FromQuery] int? audioStreamIndex,
            [FromQuery] int? subtitleStreamIndex,
            [FromQuery] int? maxAudioChannels,
            [FromQuery] string? mediaSourceId,
            [FromQuery] string? liveStreamId,
            [FromBody] DeviceProfileDto? deviceProfile,
            [FromQuery] bool autoOpenLiveStream = false,
            [FromQuery] bool enableDirectPlay = true,
            [FromQuery] bool enableDirectStream = true,
            [FromQuery] bool enableTranscoding = true,
            [FromQuery] bool allowVideoStreamCopy = true,
            [FromQuery] bool allowAudioStreamCopy = true)
        {
            var authInfo = _authContext.GetAuthorizationInfo(Request);

            var profile = deviceProfile?.DeviceProfile;

            _logger.LogInformation("GetPostedPlaybackInfo profile: {@Profile}", profile);

            if (profile == null)
            {
                var caps = _deviceManager.GetCapabilities(authInfo.DeviceId);
                if (caps != null)
                {
                    profile = caps.DeviceProfile;
                }
            }

            var info = await GetPlaybackInfoInternal(itemId, userId, mediaSourceId, liveStreamId).ConfigureAwait(false);

            if (profile != null)
            {
                // set device specific data
                var item = _libraryManager.GetItemById(itemId);

                foreach (var mediaSource in info.MediaSources)
                {
                    SetDeviceSpecificData(
                        item,
                        mediaSource,
                        profile,
                        authInfo,
                        maxStreamingBitrate ?? profile.MaxStreamingBitrate,
                        startTimeTicks ?? 0,
                        mediaSourceId ?? string.Empty,
                        audioStreamIndex,
                        subtitleStreamIndex,
                        maxAudioChannels,
                        info!.PlaySessionId!,
                        userId ?? Guid.Empty,
                        enableDirectPlay,
                        enableDirectStream,
                        enableTranscoding,
                        allowVideoStreamCopy,
                        allowAudioStreamCopy);
                }

                SortMediaSources(info, maxStreamingBitrate);
            }

            if (autoOpenLiveStream)
            {
                var mediaSource = string.IsNullOrWhiteSpace(mediaSourceId) ? info.MediaSources[0] : info.MediaSources.FirstOrDefault(i => string.Equals(i.Id, mediaSourceId, StringComparison.Ordinal));

                if (mediaSource != null && mediaSource.RequiresOpening && string.IsNullOrWhiteSpace(mediaSource.LiveStreamId))
                {
                    var openStreamResult = await OpenMediaSource(new LiveStreamRequest
                    {
                        AudioStreamIndex = audioStreamIndex,
                        DeviceProfile = deviceProfile?.DeviceProfile,
                        EnableDirectPlay = enableDirectPlay,
                        EnableDirectStream = enableDirectStream,
                        ItemId = itemId,
                        MaxAudioChannels = maxAudioChannels,
                        MaxStreamingBitrate = maxStreamingBitrate,
                        PlaySessionId = info.PlaySessionId,
                        StartTimeTicks = startTimeTicks,
                        SubtitleStreamIndex = subtitleStreamIndex,
                        UserId = userId ?? Guid.Empty,
                        OpenToken = mediaSource.OpenToken
                    }).ConfigureAwait(false);

                    info.MediaSources = new[] { openStreamResult.MediaSource };
                }
            }

            if (info.MediaSources != null)
            {
                foreach (var mediaSource in info.MediaSources)
                {
                    NormalizeMediaSourceContainer(mediaSource, profile!, DlnaProfileType.Video);
                }
            }

            return info;
        }

        /// <summary>
        /// Opens a media source.
        /// </summary>
        /// <param name="openToken">The open token.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="playSessionId">The play session id.</param>
        /// <param name="maxStreamingBitrate">The maximum streaming bitrate.</param>
        /// <param name="startTimeTicks">The start time in ticks.</param>
        /// <param name="audioStreamIndex">The audio stream index.</param>
        /// <param name="subtitleStreamIndex">The subtitle stream index.</param>
        /// <param name="maxAudioChannels">The maximum number of audio channels.</param>
        /// <param name="itemId">The item id.</param>
        /// <param name="deviceProfile">The device profile.</param>
        /// <param name="directPlayProtocols">The direct play protocols. Default: <see cref="MediaProtocol.Http"/>.</param>
        /// <param name="enableDirectPlay">Whether to enable direct play. Default: true.</param>
        /// <param name="enableDirectStream">Whether to enable direct stream. Default: true.</param>
        /// <response code="200">Media source opened.</response>
        /// <returns>A <see cref="Task"/> containing a <see cref="LiveStreamResponse"/>.</returns>
        [HttpPost("/LiveStreams/Open")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<LiveStreamResponse>> OpenLiveStream(
            [FromQuery] string? openToken,
            [FromQuery] Guid? userId,
            [FromQuery] string? playSessionId,
            [FromQuery] long? maxStreamingBitrate,
            [FromQuery] long? startTimeTicks,
            [FromQuery] int? audioStreamIndex,
            [FromQuery] int? subtitleStreamIndex,
            [FromQuery] int? maxAudioChannels,
            [FromQuery] Guid? itemId,
            [FromQuery] DeviceProfile? deviceProfile,
            [FromQuery] MediaProtocol[] directPlayProtocols,
            [FromQuery] bool enableDirectPlay = true,
            [FromQuery] bool enableDirectStream = true)
        {
            var request = new LiveStreamRequest
            {
                OpenToken = openToken,
                UserId = userId ?? Guid.Empty,
                PlaySessionId = playSessionId,
                MaxStreamingBitrate = maxStreamingBitrate,
                StartTimeTicks = startTimeTicks,
                AudioStreamIndex = audioStreamIndex,
                SubtitleStreamIndex = subtitleStreamIndex,
                MaxAudioChannels = maxAudioChannels,
                ItemId = itemId ?? Guid.Empty,
                DeviceProfile = deviceProfile,
                EnableDirectPlay = enableDirectPlay,
                EnableDirectStream = enableDirectStream,
                DirectPlayProtocols = directPlayProtocols ?? new[] { MediaProtocol.Http }
            };
            return await OpenMediaSource(request).ConfigureAwait(false);
        }

        /// <summary>
        /// Closes a media source.
        /// </summary>
        /// <param name="liveStreamId">The livestream id.</param>
        /// <response code="204">Livestream closed.</response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("/LiveStreams/Close")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult CloseLiveStream([FromQuery] string? liveStreamId)
        {
            _mediaSourceManager.CloseLiveStream(liveStreamId).GetAwaiter().GetResult();
            return NoContent();
        }

        /// <summary>
        /// Tests the network with a request with the size of the bitrate.
        /// </summary>
        /// <param name="size">The bitrate. Defaults to 102400.</param>
        /// <response code="200">Test buffer returned.</response>
        /// <response code="400">Size has to be a numer between 0 and 10,000,000.</response>
        /// <returns>A <see cref="FileResult"/> with specified bitrate.</returns>
        [HttpGet("/Playback/BitrateTest")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Produces(MediaTypeNames.Application.Octet)]
        public ActionResult GetBitrateTestBytes([FromQuery] int size = 102400)
        {
            const int MaxSize = 10_000_000;

            if (size <= 0)
            {
                return BadRequest($"The requested size ({size}) is equal to or smaller than 0.");
            }

            if (size > MaxSize)
            {
                return BadRequest($"The requested size ({size}) is larger than the max allowed value ({MaxSize}).");
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                new Random().NextBytes(buffer);
                return File(buffer, MediaTypeNames.Application.Octet);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private async Task<PlaybackInfoResponse> GetPlaybackInfoInternal(
            Guid id,
            Guid? userId,
            string? mediaSourceId = null,
            string? liveStreamId = null)
        {
            var user = userId.HasValue && !userId.Equals(Guid.Empty)
                ? _userManager.GetUserById(userId.Value)
                : null;
            var item = _libraryManager.GetItemById(id);
            var result = new PlaybackInfoResponse();

            MediaSourceInfo[] mediaSources;
            if (string.IsNullOrWhiteSpace(liveStreamId))
            {
                // TODO (moved from MediaBrowser.Api) handle supportedLiveMediaTypes?
                var mediaSourcesList = await _mediaSourceManager.GetPlaybackMediaSources(item, user, true, true, CancellationToken.None).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(mediaSourceId))
                {
                    mediaSources = mediaSourcesList.ToArray();
                }
                else
                {
                    mediaSources = mediaSourcesList
                        .Where(i => string.Equals(i.Id, mediaSourceId, StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                }
            }
            else
            {
                var mediaSource = await _mediaSourceManager.GetLiveStream(liveStreamId, CancellationToken.None).ConfigureAwait(false);

                mediaSources = new[] { mediaSource };
            }

            if (mediaSources.Length == 0)
            {
                result.MediaSources = Array.Empty<MediaSourceInfo>();

                result.ErrorCode ??= PlaybackErrorCode.NoCompatibleStream;
            }
            else
            {
                // Since we're going to be setting properties on MediaSourceInfos that come out of _mediaSourceManager, we should clone it
                // Should we move this directly into MediaSourceManager?
                result.MediaSources = JsonSerializer.Deserialize<MediaSourceInfo[]>(JsonSerializer.SerializeToUtf8Bytes(mediaSources));

                result.PlaySessionId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            }

            return result;
        }

        private void NormalizeMediaSourceContainer(MediaSourceInfo mediaSource, DeviceProfile profile, DlnaProfileType type)
        {
            mediaSource.Container = StreamBuilder.NormalizeMediaSourceFormatIntoSingleContainer(mediaSource.Container, mediaSource.Path, profile, type);
        }

        private void SetDeviceSpecificData(
            BaseItem item,
            MediaSourceInfo mediaSource,
            DeviceProfile profile,
            AuthorizationInfo auth,
            long? maxBitrate,
            long startTimeTicks,
            string mediaSourceId,
            int? audioStreamIndex,
            int? subtitleStreamIndex,
            int? maxAudioChannels,
            string playSessionId,
            Guid userId,
            bool enableDirectPlay,
            bool enableDirectStream,
            bool enableTranscoding,
            bool allowVideoStreamCopy,
            bool allowAudioStreamCopy)
        {
            var streamBuilder = new StreamBuilder(_mediaEncoder, _logger);

            var options = new VideoOptions
            {
                MediaSources = new[] { mediaSource },
                Context = EncodingContext.Streaming,
                DeviceId = auth.DeviceId,
                ItemId = item.Id,
                Profile = profile,
                MaxAudioChannels = maxAudioChannels
            };

            if (string.Equals(mediaSourceId, mediaSource.Id, StringComparison.OrdinalIgnoreCase))
            {
                options.MediaSourceId = mediaSourceId;
                options.AudioStreamIndex = audioStreamIndex;
                options.SubtitleStreamIndex = subtitleStreamIndex;
            }

            var user = _userManager.GetUserById(userId);

            if (!enableDirectPlay)
            {
                mediaSource.SupportsDirectPlay = false;
            }

            if (!enableDirectStream)
            {
                mediaSource.SupportsDirectStream = false;
            }

            if (!enableTranscoding)
            {
                mediaSource.SupportsTranscoding = false;
            }

            if (item is Audio)
            {
                _logger.LogInformation(
                    "User policy for {0}. EnableAudioPlaybackTranscoding: {1}",
                    user.Username,
                    user.HasPermission(PermissionKind.EnableAudioPlaybackTranscoding));
            }
            else
            {
                _logger.LogInformation(
                    "User policy for {0}. EnablePlaybackRemuxing: {1} EnableVideoPlaybackTranscoding: {2} EnableAudioPlaybackTranscoding: {3}",
                    user.Username,
                    user.HasPermission(PermissionKind.EnablePlaybackRemuxing),
                    user.HasPermission(PermissionKind.EnableVideoPlaybackTranscoding),
                    user.HasPermission(PermissionKind.EnableAudioPlaybackTranscoding));
            }

            // Beginning of Playback Determination: Attempt DirectPlay first
            if (mediaSource.SupportsDirectPlay)
            {
                if (mediaSource.IsRemote && user.HasPermission(PermissionKind.ForceRemoteSourceTranscoding))
                {
                    mediaSource.SupportsDirectPlay = false;
                }
                else
                {
                    var supportsDirectStream = mediaSource.SupportsDirectStream;

                    // Dummy this up to fool StreamBuilder
                    mediaSource.SupportsDirectStream = true;
                    options.MaxBitrate = maxBitrate;

                    if (item is Audio)
                    {
                        if (!user.HasPermission(PermissionKind.EnableAudioPlaybackTranscoding))
                        {
                            options.ForceDirectPlay = true;
                        }
                    }
                    else if (item is Video)
                    {
                        if (!user.HasPermission(PermissionKind.EnableAudioPlaybackTranscoding)
                            && !user.HasPermission(PermissionKind.EnableVideoPlaybackTranscoding)
                            && !user.HasPermission(PermissionKind.EnablePlaybackRemuxing))
                        {
                            options.ForceDirectPlay = true;
                        }
                    }

                    // The MediaSource supports direct stream, now test to see if the client supports it
                    var streamInfo = string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase)
                        ? streamBuilder.BuildAudioItem(options)
                        : streamBuilder.BuildVideoItem(options);

                    if (streamInfo == null || !streamInfo.IsDirectStream)
                    {
                        mediaSource.SupportsDirectPlay = false;
                    }

                    // Set this back to what it was
                    mediaSource.SupportsDirectStream = supportsDirectStream;

                    if (streamInfo != null)
                    {
                        SetDeviceSpecificSubtitleInfo(streamInfo, mediaSource, auth.Token);
                    }
                }
            }

            if (mediaSource.SupportsDirectStream)
            {
                if (mediaSource.IsRemote && user.HasPermission(PermissionKind.ForceRemoteSourceTranscoding))
                {
                    mediaSource.SupportsDirectStream = false;
                }
                else
                {
                    options.MaxBitrate = GetMaxBitrate(maxBitrate, user);

                    if (item is Audio)
                    {
                        if (!user.HasPermission(PermissionKind.EnableAudioPlaybackTranscoding))
                        {
                            options.ForceDirectStream = true;
                        }
                    }
                    else if (item is Video)
                    {
                        if (!user.HasPermission(PermissionKind.EnableAudioPlaybackTranscoding)
                            && !user.HasPermission(PermissionKind.EnableVideoPlaybackTranscoding)
                            && !user.HasPermission(PermissionKind.EnablePlaybackRemuxing))
                        {
                            options.ForceDirectStream = true;
                        }
                    }

                    // The MediaSource supports direct stream, now test to see if the client supports it
                    var streamInfo = string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase)
                        ? streamBuilder.BuildAudioItem(options)
                        : streamBuilder.BuildVideoItem(options);

                    if (streamInfo == null || !streamInfo.IsDirectStream)
                    {
                        mediaSource.SupportsDirectStream = false;
                    }

                    if (streamInfo != null)
                    {
                        SetDeviceSpecificSubtitleInfo(streamInfo, mediaSource, auth.Token);
                    }
                }
            }

            if (mediaSource.SupportsTranscoding)
            {
                options.MaxBitrate = GetMaxBitrate(maxBitrate, user);

                // The MediaSource supports direct stream, now test to see if the client supports it
                var streamInfo = string.Equals(item.MediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase)
                    ? streamBuilder.BuildAudioItem(options)
                    : streamBuilder.BuildVideoItem(options);

                if (mediaSource.IsRemote && user.HasPermission(PermissionKind.ForceRemoteSourceTranscoding))
                {
                    if (streamInfo != null)
                    {
                        streamInfo.PlaySessionId = playSessionId;
                        streamInfo.StartPositionTicks = startTimeTicks;
                        mediaSource.TranscodingUrl = streamInfo.ToUrl("-", auth.Token).TrimStart('-');
                        mediaSource.TranscodingUrl += "&allowVideoStreamCopy=false";
                        mediaSource.TranscodingUrl += "&allowAudioStreamCopy=false";
                        mediaSource.TranscodingContainer = streamInfo.Container;
                        mediaSource.TranscodingSubProtocol = streamInfo.SubProtocol;

                        // Do this after the above so that StartPositionTicks is set
                        SetDeviceSpecificSubtitleInfo(streamInfo, mediaSource, auth.Token);
                    }
                }
                else
                {
                    if (streamInfo != null)
                    {
                        streamInfo.PlaySessionId = playSessionId;

                        if (streamInfo.PlayMethod == PlayMethod.Transcode)
                        {
                            streamInfo.StartPositionTicks = startTimeTicks;
                            mediaSource.TranscodingUrl = streamInfo.ToUrl("-", auth.Token).TrimStart('-');

                            if (!allowVideoStreamCopy)
                            {
                                mediaSource.TranscodingUrl += "&allowVideoStreamCopy=false";
                            }

                            if (!allowAudioStreamCopy)
                            {
                                mediaSource.TranscodingUrl += "&allowAudioStreamCopy=false";
                            }

                            mediaSource.TranscodingContainer = streamInfo.Container;
                            mediaSource.TranscodingSubProtocol = streamInfo.SubProtocol;
                        }

                        if (!allowAudioStreamCopy)
                        {
                            mediaSource.TranscodingUrl += "&allowAudioStreamCopy=false";
                        }

                        mediaSource.TranscodingContainer = streamInfo.Container;
                        mediaSource.TranscodingSubProtocol = streamInfo.SubProtocol;

                        // Do this after the above so that StartPositionTicks is set
                        SetDeviceSpecificSubtitleInfo(streamInfo, mediaSource, auth.Token);
                    }
                }
            }

            foreach (var attachment in mediaSource.MediaAttachments)
            {
                attachment.DeliveryUrl = string.Format(
                    CultureInfo.InvariantCulture,
                    "/Videos/{0}/{1}/Attachments/{2}",
                    item.Id,
                    mediaSource.Id,
                    attachment.Index);
            }
        }

        private async Task<LiveStreamResponse> OpenMediaSource(LiveStreamRequest request)
        {
            var authInfo = _authContext.GetAuthorizationInfo(Request);

            var result = await _mediaSourceManager.OpenLiveStream(request, CancellationToken.None).ConfigureAwait(false);

            var profile = request.DeviceProfile;
            if (profile == null)
            {
                var caps = _deviceManager.GetCapabilities(authInfo.DeviceId);
                if (caps != null)
                {
                    profile = caps.DeviceProfile;
                }
            }

            if (profile != null)
            {
                var item = _libraryManager.GetItemById(request.ItemId);

                SetDeviceSpecificData(
                    item,
                    result.MediaSource,
                    profile,
                    authInfo,
                    request.MaxStreamingBitrate,
                    request.StartTimeTicks ?? 0,
                    result.MediaSource.Id,
                    request.AudioStreamIndex,
                    request.SubtitleStreamIndex,
                    request.MaxAudioChannels,
                    request.PlaySessionId,
                    request.UserId,
                    request.EnableDirectPlay,
                    request.EnableDirectStream,
                    true,
                    true,
                    true);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(result.MediaSource.TranscodingUrl))
                {
                    result.MediaSource.TranscodingUrl += "&LiveStreamId=" + result.MediaSource.LiveStreamId;
                }
            }

            // here was a check if (result.MediaSource != null) but Rider said it will never be null
            NormalizeMediaSourceContainer(result.MediaSource, profile!, DlnaProfileType.Video);

            return result;
        }

        private void SetDeviceSpecificSubtitleInfo(StreamInfo info, MediaSourceInfo mediaSource, string accessToken)
        {
            var profiles = info.GetSubtitleProfiles(_mediaEncoder, false, "-", accessToken);
            mediaSource.DefaultSubtitleStreamIndex = info.SubtitleStreamIndex;

            mediaSource.TranscodeReasons = info.TranscodeReasons;

            foreach (var profile in profiles)
            {
                foreach (var stream in mediaSource.MediaStreams)
                {
                    if (stream.Type == MediaStreamType.Subtitle && stream.Index == profile.Index)
                    {
                        stream.DeliveryMethod = profile.DeliveryMethod;

                        if (profile.DeliveryMethod == SubtitleDeliveryMethod.External)
                        {
                            stream.DeliveryUrl = profile.Url.TrimStart('-');
                            stream.IsExternalUrl = profile.IsExternalUrl;
                        }
                    }
                }
            }
        }

        private long? GetMaxBitrate(long? clientMaxBitrate, User user)
        {
            var maxBitrate = clientMaxBitrate;
            var remoteClientMaxBitrate = user?.RemoteClientBitrateLimit ?? 0;

            if (remoteClientMaxBitrate <= 0)
            {
                remoteClientMaxBitrate = _serverConfigurationManager.Configuration.RemoteClientBitrateLimit;
            }

            if (remoteClientMaxBitrate > 0)
            {
                var isInLocalNetwork = _networkManager.IsInLocalNetwork(Request.HttpContext.Connection.RemoteIpAddress.ToString());

                _logger.LogInformation("RemoteClientBitrateLimit: {0}, RemoteIp: {1}, IsInLocalNetwork: {2}", remoteClientMaxBitrate, Request.HttpContext.Connection.RemoteIpAddress.ToString(), isInLocalNetwork);
                if (!isInLocalNetwork)
                {
                    maxBitrate = Math.Min(maxBitrate ?? remoteClientMaxBitrate, remoteClientMaxBitrate);
                }
            }

            return maxBitrate;
        }

        private void SortMediaSources(PlaybackInfoResponse result, long? maxBitrate)
        {
            var originalList = result.MediaSources.ToList();

            result.MediaSources = result.MediaSources.OrderBy(i =>
                {
                    // Nothing beats direct playing a file
                    if (i.SupportsDirectPlay && i.Protocol == MediaProtocol.File)
                    {
                        return 0;
                    }

                    return 1;
                })
                .ThenBy(i =>
                {
                    // Let's assume direct streaming a file is just as desirable as direct playing a remote url
                    if (i.SupportsDirectPlay || i.SupportsDirectStream)
                    {
                        return 0;
                    }

                    return 1;
                })
                .ThenBy(i =>
                {
                    return i.Protocol switch
                    {
                        MediaProtocol.File => 0,
                        _ => 1,
                    };
                })
                .ThenBy(i =>
                {
                    if (maxBitrate.HasValue && i.Bitrate.HasValue)
                    {
                        return i.Bitrate.Value <= maxBitrate.Value ? 0 : 2;
                    }

                    return 1;
                })
                .ThenBy(originalList.IndexOf)
                .ToArray();
        }
    }
}
