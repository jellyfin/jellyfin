using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Helpers;

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
    public MediaInfoHelper(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IMediaSourceManager mediaSourceManager,
        IMediaEncoder mediaEncoder,
        IServerConfigurationManager serverConfigurationManager,
        ILogger<MediaInfoHelper> logger,
        INetworkManager networkManager,
        IDeviceManager deviceManager)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _mediaSourceManager = mediaSourceManager;
        _mediaEncoder = mediaEncoder;
        _serverConfigurationManager = serverConfigurationManager;
        _logger = logger;
        _networkManager = networkManager;
        _deviceManager = deviceManager;
    }

    /// <summary>
    /// Get playback info.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="user">The user.</param>
    /// <param name="mediaSourceId">Media source id.</param>
    /// <param name="liveStreamId">Live stream id.</param>
    /// <returns>A <see cref="Task"/> containing the <see cref="PlaybackInfoResponse"/>.</returns>
    public async Task<PlaybackInfoResponse> GetPlaybackInfo(
        BaseItem item,
        User? user,
        string? mediaSourceId = null,
        string? liveStreamId = null)
    {
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
            if (mediaSourcesClone is not null)
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
    /// <param name="claimsPrincipal">Current claims principal.</param>
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
    /// <param name="alwaysBurnInSubtitleWhenTranscoding">Always burn-in subtitle when transcoding.</param>
    /// <param name="ipAddress">Requesting IP address.</param>
    public void SetDeviceSpecificData(
        BaseItem item,
        MediaSourceInfo mediaSource,
        DeviceProfile profile,
        ClaimsPrincipal claimsPrincipal,
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
        bool alwaysBurnInSubtitleWhenTranscoding,
        IPAddress ipAddress)
    {
        var streamBuilder = new StreamBuilder(_mediaEncoder, _logger);

        var options = new MediaOptions
        {
            MediaSources = new[] { mediaSource },
            Context = EncodingContext.Streaming,
            DeviceId = claimsPrincipal.GetDeviceId(),
            ItemId = item.Id,
            Profile = profile,
            MaxAudioChannels = maxAudioChannels,
            AllowAudioStreamCopy = allowAudioStreamCopy,
            AllowVideoStreamCopy = allowVideoStreamCopy,
            AlwaysBurnInSubtitleWhenTranscoding = alwaysBurnInSubtitleWhenTranscoding,
        };

        if (string.Equals(mediaSourceId, mediaSource.Id, StringComparison.OrdinalIgnoreCase))
        {
            options.MediaSourceId = mediaSourceId;
            options.AudioStreamIndex = audioStreamIndex;
            options.SubtitleStreamIndex = subtitleStreamIndex;
        }

        var user = _userManager.GetUserById(userId) ?? throw new ResourceNotFoundException();

        if (!enableDirectPlay)
        {
            mediaSource.SupportsDirectPlay = false;
        }

        if (!enableDirectStream || !allowVideoStreamCopy)
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

        options.MaxBitrate = GetMaxBitrate(maxBitrate, user, ipAddress);

        if (!options.ForceDirectStream)
        {
            // direct-stream http streaming is currently broken
            options.EnableDirectStream = false;
        }

        // Beginning of Playback Determination
        var streamInfo = item.MediaType == MediaType.Audio
            ? streamBuilder.GetOptimalAudioStream(options)
            : streamBuilder.GetOptimalVideoStream(options);

        if (streamInfo is not null)
        {
            streamInfo.PlaySessionId = playSessionId;
            streamInfo.StartPositionTicks = startTimeTicks;

            mediaSource.SupportsDirectPlay = streamInfo.PlayMethod == PlayMethod.DirectPlay;

            // Players do not handle this being set according to PlayMethod
            mediaSource.SupportsDirectStream =
                options.EnableDirectStream
                    ? streamInfo.PlayMethod == PlayMethod.DirectPlay || streamInfo.PlayMethod == PlayMethod.DirectStream
                    : streamInfo.PlayMethod == PlayMethod.DirectPlay;

            mediaSource.SupportsTranscoding =
                streamInfo.PlayMethod == PlayMethod.DirectStream
                || mediaSource.TranscodingContainer is not null
                || profile.TranscodingProfiles.Any(i => i.Type == streamInfo.MediaType && i.Context == options.Context);

            if (item is Audio)
            {
                if (!user.HasPermission(PermissionKind.EnableAudioPlaybackTranscoding))
                {
                    mediaSource.SupportsTranscoding = false;
                }
            }
            else if (item is Video)
            {
                if (!user.HasPermission(PermissionKind.EnableAudioPlaybackTranscoding)
                    && !user.HasPermission(PermissionKind.EnableVideoPlaybackTranscoding)
                    && !user.HasPermission(PermissionKind.EnablePlaybackRemuxing))
                {
                    mediaSource.SupportsTranscoding = false;
                }
            }

            if (mediaSource.IsRemote && user.HasPermission(PermissionKind.ForceRemoteSourceTranscoding))
            {
                mediaSource.SupportsDirectPlay = false;
                mediaSource.SupportsDirectStream = false;

                mediaSource.TranscodingUrl = streamInfo.ToUrl("-", claimsPrincipal.GetToken()).TrimStart('-');
                mediaSource.TranscodingUrl += "&allowVideoStreamCopy=false";
                mediaSource.TranscodingUrl += "&allowAudioStreamCopy=false";
                mediaSource.TranscodingContainer = streamInfo.Container;
                mediaSource.TranscodingSubProtocol = streamInfo.SubProtocol;
                if (streamInfo.AlwaysBurnInSubtitleWhenTranscoding)
                {
                    mediaSource.TranscodingUrl += "&alwaysBurnInSubtitleWhenTranscoding=true";
                }
            }
            else
            {
                if (!mediaSource.SupportsDirectPlay && (mediaSource.SupportsTranscoding || mediaSource.SupportsDirectStream))
                {
                    streamInfo.PlayMethod = PlayMethod.Transcode;
                    mediaSource.TranscodingUrl = streamInfo.ToUrl("-", claimsPrincipal.GetToken()).TrimStart('-');

                    if (!allowVideoStreamCopy)
                    {
                        mediaSource.TranscodingUrl += "&allowVideoStreamCopy=false";
                    }

                    if (!allowAudioStreamCopy)
                    {
                        mediaSource.TranscodingUrl += "&allowAudioStreamCopy=false";
                    }

                    if (streamInfo.AlwaysBurnInSubtitleWhenTranscoding)
                    {
                        mediaSource.TranscodingUrl += "&alwaysBurnInSubtitleWhenTranscoding=true";
                    }
                }
            }

            // Do this after the above so that StartPositionTicks is set
            // The token must not be null
            SetDeviceSpecificSubtitleInfo(streamInfo, mediaSource, claimsPrincipal.GetToken()!);
            mediaSource.DefaultAudioStreamIndex = streamInfo.AudioStreamIndex;
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
    /// <param name="httpContext">Http Context.</param>
    /// <param name="request">Live stream request.</param>
    /// <returns>A <see cref="Task"/> containing the <see cref="LiveStreamResponse"/>.</returns>
    public async Task<LiveStreamResponse> OpenMediaSource(HttpContext httpContext, LiveStreamRequest request)
    {
        var result = await _mediaSourceManager.OpenLiveStream(request, CancellationToken.None).ConfigureAwait(false);

        var profile = request.DeviceProfile;
        if (profile is null)
        {
            var clientCapabilities = _deviceManager.GetCapabilities(httpContext.User.GetDeviceId());
            if (clientCapabilities is not null)
            {
                profile = clientCapabilities.DeviceProfile;
            }
        }

        if (profile is not null)
        {
            var item = _libraryManager.GetItemById<BaseItem>(request.ItemId)
                ?? throw new ResourceNotFoundException();

            SetDeviceSpecificData(
                item,
                result.MediaSource,
                profile,
                httpContext.User,
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
                request.AlwaysBurnInSubtitleWhenTranscoding,
                httpContext.GetNormalizedRemoteIP());
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(result.MediaSource.TranscodingUrl))
            {
                result.MediaSource.TranscodingUrl += "&LiveStreamId=" + result.MediaSource.LiveStreamId;
            }
        }

        // here was a check if (result.MediaSource is not null) but Rider said it will never be null
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
        mediaSource.Container = StreamBuilder.NormalizeMediaSourceFormatIntoSingleContainer(mediaSource.Container, profile, type);
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

    private int? GetMaxBitrate(int? clientMaxBitrate, User user, IPAddress ipAddress)
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

            _logger.LogInformation("RemoteClientBitrateLimit: {0}, RemoteIP: {1}, IsInLocalNetwork: {2}", remoteClientMaxBitrate, ipAddress, isInLocalNetwork);
            if (!isInLocalNetwork)
            {
                maxBitrate = Math.Min(maxBitrate ?? remoteClientMaxBitrate, remoteClientMaxBitrate);
            }
        }

        return maxBitrate;
    }
}
