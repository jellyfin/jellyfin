using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Extensions;
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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Media info helper.
    /// </summary>
    public class MediaInfoHelper
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly ILogger<MediaInfoHelper> _logger;
        private readonly INetworkManager _networkManager;
        private readonly IDeviceManager _deviceManager;
        private readonly IAuthorizationContext _authContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaInfoHelper"/> class.
        /// </summary>
        /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// <param name="mediaSourceManager">Instance of the <see cref="IMediaSourceManager"/> interface.</param>
        /// <param name="mediaEncoder">Instance of the <see cref="IMediaEncoder"/> interface.</param>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{MediaInfoHelper}"/> interface.</param>
        /// <param name="networkManager">Instance of the <see cref="INetworkManager"/> interface.</param>
        /// <param name="deviceManager">Instance of the <see cref="IDeviceManager"/> interface.</param>
        /// <param name="authContext">Instance of the <see cref="IAuthorizationContext"/> interface.</param>
        public MediaInfoHelper(
            IUserManager userManager,
            ILibraryManager libraryManager,
            IMediaSourceManager mediaSourceManager,
            IMediaEncoder mediaEncoder,
            IServerConfigurationManager serverConfigurationManager,
            ILogger<MediaInfoHelper> logger,
            INetworkManager networkManager,
            IDeviceManager deviceManager,
            IAuthorizationContext authContext)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _mediaSourceManager = mediaSourceManager;
            _mediaEncoder = mediaEncoder;
            _serverConfigurationManager = serverConfigurationManager;
            _logger = logger;
            _networkManager = networkManager;
            _deviceManager = deviceManager;
            _authContext = authContext;
        }

        /// <summary>
        /// Get playback info.
        /// </summary>
        /// <param name="id">Item id.</param>
        /// <param name="userId">User Id.</param>
        /// <param name="mediaSourceId">Media source id.</param>
        /// <param name="liveStreamId">Live stream id.</param>
        /// <returns>A <see cref="Task"/> containing the <see cref="PlaybackInfoResponse"/>.</returns>
        public async Task<PlaybackInfoResponse> GetPlaybackInfo(
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
                var mediaSourcesClone = JsonSerializer.Deserialize<MediaSourceInfo[]>(JsonSerializer.SerializeToUtf8Bytes(mediaSources));
                if (mediaSourcesClone != null)
                {
                    result.MediaSources = mediaSourcesClone;
                }

                result.PlaySessionId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            }

            return result;
        }

        /// <summary>
        /// SetDeviceSpecificData.
        /// </summary>
        /// <param name="item">Item to set data for.</param>
        /// <param name="mediaSource">Media source info.</param>
        /// <param name="profile">Device profile.</param>
        /// <param name="auth">Authorization info.</param>
        /// <param name="maxBitrate">Max bitrate.</param>
        /// <param name="startTimeTicks">Start time ticks.</param>
        /// <param name="mediaSourceId">Media source id.</param>
        /// <param name="audioStreamIndex">Audio stream index.</param>
        /// <param name="subtitleStreamIndex">Subtitle stream index.</param>
        /// <param name="maxAudioChannels">Max audio channels.</param>
        /// <param name="playSessionId">Play session id.</param>
        /// <param name="userId">User id.</param>
        /// <param name="enableDirectPlay">Enable direct play.</param>
        /// <param name="enableDirectStream">Enable direct stream.</param>
        /// <param name="enableTranscoding">Enable transcoding.</param>
        /// <param name="allowVideoStreamCopy">Allow video stream copy.</param>
        /// <param name="allowAudioStreamCopy">Allow audio stream copy.</param>
        /// <param name="ipAddress">Requesting IP address.</param>
        public void SetDeviceSpecificData(
            BaseItem item,
            MediaSourceInfo mediaSource,
            DeviceProfile profile,
            AuthorizationInfo auth,
            int? maxBitrate,
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
            bool allowAudioStreamCopy,
            string ipAddress)
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
                        mediaSource.DefaultAudioStreamIndex = streamInfo.AudioStreamIndex;
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
                    options.MaxBitrate = GetMaxBitrate(maxBitrate, user, ipAddress);

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
                        mediaSource.DefaultAudioStreamIndex = streamInfo.AudioStreamIndex;
                    }
                }
            }

            if (mediaSource.SupportsTranscoding)
            {
                options.MaxBitrate = GetMaxBitrate(maxBitrate, user, ipAddress);

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
                        mediaSource.DefaultAudioStreamIndex = streamInfo.AudioStreamIndex;
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
                        mediaSource.DefaultAudioStreamIndex = streamInfo.AudioStreamIndex;
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

        /// <summary>
        /// Sort media source.
        /// </summary>
        /// <param name="result">Playback info response.</param>
        /// <param name="maxBitrate">Max bitrate.</param>
        public void SortMediaSources(PlaybackInfoResponse result, long? maxBitrate)
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

        /// <summary>
        /// Open media source.
        /// </summary>
        /// <param name="httpRequest">Http Request.</param>
        /// <param name="request">Live stream request.</param>
        /// <returns>A <see cref="Task"/> containing the <see cref="LiveStreamResponse"/>.</returns>
        public async Task<LiveStreamResponse> OpenMediaSource(HttpRequest httpRequest, LiveStreamRequest request)
        {
            var authInfo = _authContext.GetAuthorizationInfo(httpRequest);

            var result = await _mediaSourceManager.OpenLiveStream(request, CancellationToken.None).ConfigureAwait(false);

            var profile = request.DeviceProfile;
            if (profile == null)
            {
                var clientCapabilities = _deviceManager.GetCapabilities(authInfo.DeviceId);
                if (clientCapabilities != null)
                {
                    profile = clientCapabilities.DeviceProfile;
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
                    true,
                    httpRequest.HttpContext.GetNormalizedRemoteIp());
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

        /// <summary>
        /// Normalize media source container.
        /// </summary>
        /// <param name="mediaSource">Media source.</param>
        /// <param name="profile">Device profile.</param>
        /// <param name="type">Dlna profile type.</param>
        public void NormalizeMediaSourceContainer(MediaSourceInfo mediaSource, DeviceProfile profile, DlnaProfileType type)
        {
            mediaSource.Container = StreamBuilder.NormalizeMediaSourceFormatIntoSingleContainer(mediaSource.Container, mediaSource.Path, profile, type);
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

        private int? GetMaxBitrate(int? clientMaxBitrate, User user, string ipAddress)
        {
            var maxBitrate = clientMaxBitrate;
            var remoteClientMaxBitrate = user.RemoteClientBitrateLimit ?? 0;

            if (remoteClientMaxBitrate <= 0)
            {
                remoteClientMaxBitrate = _serverConfigurationManager.Configuration.RemoteClientBitrateLimit;
            }

            if (remoteClientMaxBitrate > 0)
            {
                var isInLocalNetwork = _networkManager.IsInLocalNetwork(ipAddress);

                _logger.LogInformation("RemoteClientBitrateLimit: {0}, RemoteIp: {1}, IsInLocalNetwork: {2}", remoteClientMaxBitrate, ipAddress, isInLocalNetwork);
                if (!isInLocalNetwork)
                {
                    maxBitrate = Math.Min(maxBitrate ?? remoteClientMaxBitrate, remoteClientMaxBitrate);
                }
            }

            return maxBitrate;
        }
    }
}
