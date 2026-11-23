using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;
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
    private readonly IServerApplicationHost _appHost;
    private readonly VideoPlaybackHelper? _videoPlaybackHelper;
    private readonly ILiveStreamPlaybackManager? _liveStreams;
    private readonly ISessionManager? _sessionManager;

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
    /// <param name="appHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
    public MediaInfoHelper(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IMediaSourceManager mediaSourceManager,
        IMediaEncoder mediaEncoder,
        IServerConfigurationManager serverConfigurationManager,
        ILogger<MediaInfoHelper> logger,
        INetworkManager networkManager,
        IDeviceManager deviceManager,
        IServerApplicationHost appHost)
        : this(userManager, libraryManager, mediaSourceManager, mediaEncoder, serverConfigurationManager, logger, networkManager, deviceManager, appHost, null, null, null)
    {
    }

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
    /// <param name="appHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
    /// <param name="videoPlaybackHelper">The authenticated playback verifier.</param>
    /// <param name="liveStreams">The ownership of opened inputs.</param>
    /// <param name="sessionManager">The current server sessions.</param>
    public MediaInfoHelper(
        IUserManager userManager,
        ILibraryManager libraryManager,
        IMediaSourceManager mediaSourceManager,
        IMediaEncoder mediaEncoder,
        IServerConfigurationManager serverConfigurationManager,
        ILogger<MediaInfoHelper> logger,
        INetworkManager networkManager,
        IDeviceManager deviceManager,
        IServerApplicationHost appHost,
        VideoPlaybackHelper? videoPlaybackHelper,
        ILiveStreamPlaybackManager? liveStreams,
        ISessionManager? sessionManager)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _mediaSourceManager = mediaSourceManager;
        _mediaEncoder = mediaEncoder;
        _serverConfigurationManager = serverConfigurationManager;
        _logger = logger;
        _networkManager = networkManager;
        _deviceManager = deviceManager;
        _appHost = appHost;
        _videoPlaybackHelper = videoPlaybackHelper;
        _liveStreams = liveStreams;
        _sessionManager = sessionManager;
    }

    /// <summary>
    /// Get playback info.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="user">The user.</param>
    /// <param name="request">The current <see cref="HttpRequest"/>.</param>
    /// <param name="mediaSourceId">Media source id.</param>
    /// <param name="liveStreamId">Live stream id.</param>
    /// <returns>A <see cref="Task"/> containing the <see cref="PlaybackInfoResponse"/>.</returns>
    public async Task<PlaybackInfoResponse> GetPlaybackInfo(
        BaseItem item,
        User? user,
        HttpRequest request,
        string? mediaSourceId = null,
        string? liveStreamId = null)
    {
        var result = new PlaybackInfoResponse();

        if (!string.IsNullOrWhiteSpace(liveStreamId))
        {
            _videoPlaybackHelper?.VerifyLiveStream(request.HttpContext, item.Id, liveStreamId, isVideoOutput: false);
        }

        var mediaSources = await ResolvePlaybackMediaSources(item, user, mediaSourceId, liveStreamId).ConfigureAwait(false);

        if (mediaSources.Length == 0)
        {
            result.MediaSources = Array.Empty<MediaSourceInfo>();

            result.ErrorCode ??= PlaybackErrorCode.NoCompatibleStream;
        }
        else
        {
            result.PlaySessionId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            // Since we're going to be setting properties on MediaSourceInfos that come out of _mediaSourceManager, we should clone it
            // Should we move this directly into MediaSourceManager?
            var mediaSourcesClone = JsonSerializer.Deserialize<MediaSourceInfo[]>(JsonSerializer.SerializeToUtf8Bytes(mediaSources));
            if (mediaSourcesClone is not null)
            {
                // Carry over the default audio index source.
                // This field is not intended to be exposed to API clients, but it is used internally by the server
                for (int i = 0; i < mediaSourcesClone.Length && i < mediaSources.Length; i++)
                {
                    mediaSourcesClone[i].DefaultAudioIndexSource = mediaSources[i].DefaultAudioIndexSource;
                }

                foreach (var mediaSource in mediaSourcesClone)
                {
                    RewritePublishedLiveStreamPath(mediaSource, request, item.Id, result.PlaySessionId);
                }

                result.MediaSources = mediaSourcesClone;
            }
        }

        return result;
    }

    private async Task<MediaSourceInfo[]> ResolvePlaybackMediaSources(BaseItem item, User? user, string? mediaSourceId, string? liveStreamId)
    {
        if (!string.IsNullOrWhiteSpace(liveStreamId))
        {
            var mediaSource = await _mediaSourceManager.GetLiveStream(liveStreamId, CancellationToken.None).ConfigureAwait(false);

            return new[] { mediaSource };
        }

        // TODO (moved from MediaBrowser.Api) handle supportedLiveMediaTypes?
        var mediaSourcesList = await _mediaSourceManager.GetPlaybackMediaSources(item, user, true, true, CancellationToken.None).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(mediaSourceId))
        {
            return mediaSourcesList.ToArray();
        }

        return mediaSourcesList
            .Where(i => string.Equals(i.Id, mediaSourceId, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    /// <summary>
    /// Gives a new logical playback unopened sources instead of borrowing another input consumer.
    /// </summary>
    /// <param name="context">The authenticated request.</param>
    /// <param name="item">The canonical item.</param>
    /// <param name="user">The selected metadata preferences.</param>
    /// <param name="info">The registered playback metadata.</param>
    /// <returns>The asynchronous source refresh.</returns>
    public async Task EnsurePlaybackSources(HttpContext context, BaseItem item, User? user, PlaybackInfoResponse info)
    {
        var owner = context.User.GetPlaybackOwnerId();
        if (_liveStreams is null || info.PlaySessionId is null || owner is null || context.User.GetIsApiKey())
        {
            return;
        }

        if (info.MediaSources.Any(source => !string.IsNullOrEmpty(source.LiveStreamId)
            && !_liveStreams.Verify(context.User.GetUserId(), owner, item.Id, source.LiveStreamId, info.PlaySessionId)))
        {
            var refreshed = await GetPlaybackInfo(item, user, context.Request).ConfigureAwait(false);
            info.MediaSources = refreshed.MediaSources;
        }
    }

    /// <summary>
    /// Routes remote direct play through the server when the authenticated user has a video limit.
    /// </summary>
    /// <param name="context">The authenticated metadata request.</param>
    /// <param name="itemId">The canonical library item.</param>
    /// <param name="itemMediaType">The canonical media type.</param>
    /// <param name="playSessionId">The registered encoding identifier.</param>
    /// <param name="sources">The response copies of the media sources.</param>
    public void RewriteBoundedRemoteSources(HttpContext context, Guid itemId, MediaType itemMediaType, string? playSessionId, System.Collections.Generic.IEnumerable<MediaSourceInfo> sources)
    {
        if (itemMediaType != MediaType.Video || string.IsNullOrWhiteSpace(playSessionId) || context.User.GetIsApiKey()
            || _userManager.GetUserById(context.User.GetUserId()) is not { MaxActiveVideoStreams: > 0 })
        {
            return;
        }

        foreach (var source in sources)
        {
            if (source.Protocol != MediaProtocol.Http)
            {
                continue;
            }

            var endpoint = _appHost.GetSmartApiUrl(context.Request).TrimEnd('/') + "/Videos/" + itemId.ToString("N", CultureInfo.InvariantCulture) + "/stream";
            source.Path = QueryHelpers.AddQueryString(endpoint, new System.Collections.Generic.Dictionary<string, string?>
            {
                ["Static"] = "true",
                ["PlaySessionId"] = playSessionId,
                ["MediaSourceId"] = source.Id,
                ["LiveStreamId"] = source.LiveStreamId,
                ["ApiKey"] = context.User.GetToken()
            });
            source.EncoderPath = null;
            source.EncoderProtocol = null;
            source.RequiredHttpHeaders.Clear();
        }
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

                mediaSource.TranscodingUrl = streamInfo.ToUrl(null, claimsPrincipal.GetToken(), "&allowVideoStreamCopy=false&allowAudioStreamCopy=false");
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
                    mediaSource.TranscodingUrl = streamInfo.ToUrl(null, claimsPrincipal.GetToken(), null);

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
    /// <param name="preferredItemId">The id of the queried item, whose own media source must stay the default.</param>
    public void SortMediaSources(PlaybackInfoResponse result, long? maxBitrate, Guid preferredItemId = default)
    {
        var originalList = result.MediaSources.ToList();

        // The queried item's source carries the user's resume state for that version, so it must stay the
        // default the client plays. An unfavorable bitrate means transcoding it, not switching to a sibling version.
        var preferredId = preferredItemId.IsEmpty()
            ? null
            : preferredItemId.ToString("N", CultureInfo.InvariantCulture);

        result.MediaSources = result.MediaSources
            .OrderByDescending(i => preferredId is not null && string.Equals(i.Id, preferredId, StringComparison.OrdinalIgnoreCase))
            .ThenBy(i =>
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
        var opened = await OpenVerifiedMediaSource(httpContext, request).ConfigureAwait(false);
        var result = opened.Response;

        try
        {
            RewritePublishedLiveStreamPath(result.MediaSource, httpContext.Request, request.ItemId, request.PlaySessionId);

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

            var canonicalItem = _libraryManager.GetItemById<BaseItem>(request.ItemId);
            if (canonicalItem is not null)
            {
                RewriteBoundedRemoteSources(httpContext, canonicalItem.Id, canonicalItem.MediaType, request.PlaySessionId, new[] { result.MediaSource });
            }

            return result;
        }
        catch
        {
            var owner = httpContext.User.GetPlaybackOwnerId();
            if (opened.OwnsNewConsumer)
            {
                if (_liveStreams is not null && !string.IsNullOrEmpty(owner))
                {
                    await _liveStreams.CloseOwnedAsync(httpContext.User.GetUserId(), owner, result.MediaSource.LiveStreamId, request.PlaySessionId).ConfigureAwait(false);
                }
                else
                {
                    await _mediaSourceManager.CloseLiveStream(result.MediaSource.LiveStreamId).ConfigureAwait(false);
                }
            }

            throw;
        }
    }

    private async Task<(LiveStreamResponse Response, bool OwnsNewConsumer)> OpenVerifiedMediaSource(HttpContext context, LiveStreamRequest request)
    {
        // Retain the original helper construction contract for server extensions.
        if (_videoPlaybackHelper is null || _liveStreams is null || _sessionManager is null)
        {
            return (await _mediaSourceManager.OpenLiveStream(request, context.RequestAborted).ConfigureAwait(false), true);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(request.OpenToken);
        var userId = context.User.GetUserId();
        var user = userId.Equals(Guid.Empty) ? null : _userManager.GetUserById(userId);
        var requiresAudioToken = false;
        if (request.ItemId.Equals(Guid.Empty) && !string.IsNullOrWhiteSpace(request.PlaySessionId))
        {
            requiresAudioToken = !_videoPlaybackHelper.TryResolvePlayback(context, null, request.PlaySessionId, out var playback);
            if (!requiresAudioToken)
            {
                request.ItemId = playback.ItemId;
            }
        }

        BaseItem? item = null;
        if (!request.ItemId.Equals(Guid.Empty))
        {
            item = _libraryManager.GetItemById<BaseItem>(request.ItemId, user);
        }
        else
        {
            // Built-in legacy OpenTokens contain the item id. Resolve it only after
            // proving that this exact token is advertised by an accessible item.
            foreach (var part in request.OpenToken.Split('_', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!Guid.TryParse(part, out var candidate))
                {
                    continue;
                }

                var candidateItem = _libraryManager.GetItemById<BaseItem>(candidate, user);
                if (candidateItem is not null
                    && (!requiresAudioToken || candidateItem.MediaType == MediaType.Audio)
                    && await HasOpenToken(candidateItem, user, request.OpenToken, context.RequestAborted).ConfigureAwait(false))
                {
                    item = candidateItem;
                    request.ItemId = item.Id;
                    break;
                }
            }
        }

        if (item is null)
        {
            if (requiresAudioToken)
            {
                throw new SecurityException("The live source has no verified playback alias or audio open token.");
            }

            throw new ResourceNotFoundException("An accessible live playback item is required.");
        }

        if (!await HasOpenToken(item, user, request.OpenToken, context.RequestAborted).ConfigureAwait(false))
        {
            throw new SecurityException("The live source does not belong to the requested item.");
        }

        using var operation = _videoPlaybackHelper.Acquire(context, item.Id, request.PlaySessionId, out var alias);
        request.PlaySessionId = alias ?? request.PlaySessionId ?? Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var owner = context.User.GetPlaybackOwnerId();
        var managedOwner = !userId.Equals(Guid.Empty) && !string.IsNullOrEmpty(owner);
        if (managedOwner && _liveStreams.TryGetLiveStream(userId, owner!, item.Id, request.PlaySessionId, out var existingId))
        {
            var existing = await _mediaSourceManager.GetLiveStream(existingId, context.RequestAborted).ConfigureAwait(false);
            var clone = JsonSerializer.Deserialize<MediaSourceInfo>(JsonSerializer.SerializeToUtf8Bytes(existing))!;
            clone.DefaultAudioIndexSource = existing.DefaultAudioIndexSource;
            return (new LiveStreamResponse(clone), false);
        }

        var result = await _mediaSourceManager.OpenLiveStream(request, context.RequestAborted).ConfigureAwait(false);
        if (!managedOwner)
        {
            return (result, true);
        }

        var duplicateReleased = false;
        try
        {
            var playback = operation is null
                ? new VideoPlaybackContext(userId, owner!, item.Id, request.PlaySessionId)
                : _videoPlaybackHelper.ResolvePlayback(context, item.Id, request.PlaySessionId);
            var sessionId = await RequestHelpers.GetSessionId(_sessionManager, _userManager, context).ConfigureAwait(false);
            if (!_liveStreams.Register(playback, request.PlaySessionId, result.MediaSource.LiveStreamId, sessionId, operation is not null))
            {
                // Concurrent equivalent opens must not retain a second provider consumer.
                duplicateReleased = true;
                await _mediaSourceManager.CloseLiveStream(result.MediaSource.LiveStreamId).ConfigureAwait(false);
            }
        }
        catch
        {
            if (!duplicateReleased)
            {
                await _mediaSourceManager.CloseLiveStream(result.MediaSource.LiveStreamId).ConfigureAwait(false);
            }

            throw;
        }

        return (result, !duplicateReleased);
    }

    private async Task<bool> HasOpenToken(BaseItem item, User? user, string openToken, CancellationToken cancellationToken)
    {
        var sources = await _mediaSourceManager.GetPlaybackMediaSources(item, user, true, false, cancellationToken).ConfigureAwait(false);
        return sources.Any(source => string.Equals(source.OpenToken, openToken, StringComparison.Ordinal));
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

    /// <summary>
    /// Rewrites a Live TV media source's <see cref="MediaSourceInfo.Path"/> to the request-appropriate published
    /// URL when it points at a Jellyfin-hosted live stream buffer, so response copies never leak server-local
    /// addresses. Only opened live streams are eligible. The shared instance held by
    /// <see cref="IMediaSourceManager"/> is never touched by this method.
    /// </summary>
    /// <param name="mediaSource">The media source clone to rewrite in place.</param>
    /// <param name="request">The current <see cref="HttpRequest"/>.</param>
    /// <param name="itemId">The canonical item for the authenticated streaming URL.</param>
    /// <param name="playSessionId">The verified playback alias.</param>
    private void RewritePublishedLiveStreamPath(MediaSourceInfo mediaSource, HttpRequest request, Guid itemId = default, string? playSessionId = null)
    {
        // Opened live streams always carry a LiveStreamId; this excludes pre-open and plugin/remote sources.
        if (string.IsNullOrEmpty(mediaSource.LiveStreamId))
        {
            return;
        }

        var baseUrl = _serverConfigurationManager.GetNetworkConfiguration().BaseUrl;
        var publishedPath = GetPublishedLiveStreamPath(_appHost.GetSmartApiUrl(request), mediaSource.Path, mediaSource.Protocol, baseUrl);

        if (publishedPath is not null)
        {
            mediaSource.Path = publishedPath;
            if (_videoPlaybackHelper is not null && !itemId.Equals(Guid.Empty) && !string.IsNullOrWhiteSpace(playSessionId))
            {
                var marker = publishedPath.IndexOf("/LiveTv/LiveStreamFiles/", StringComparison.OrdinalIgnoreCase);
                var endpoint = publishedPath[..marker] + "/Videos/" + itemId.ToString("N", CultureInfo.InvariantCulture) + "/stream.ts";
                mediaSource.Path = QueryHelpers.AddQueryString(endpoint, new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["Static"] = "true",
                    ["LiveStreamId"] = mediaSource.LiveStreamId,
                    ["PlaySessionId"] = playSessionId,
                    ["MediaSourceId"] = mediaSource.Id,
                    ["ApiKey"] = request.HttpContext.User.GetToken()
                });
                mediaSource.EncoderPath = null;
                mediaSource.EncoderProtocol = null;
            }

            return;
        }

        if (mediaSource.Path is not null && mediaSource.Path.Contains("/LiveTv/LiveStreamFiles/", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Not rewriting live stream path for media source {MediaSourceId}: the local path did not resolve under the request's smart API URL/BaseUrl", mediaSource.Id);
        }
    }

    /// <summary>
    /// Resolves a Jellyfin-hosted Live TV buffer path to its request-appropriate published equivalent.
    /// Returns null when the path isn't a Jellyfin-hosted <c>/LiveTv/LiveStreamFiles/</c> HTTP URL.
    /// </summary>
    /// <param name="smartApiUrl">The request-appropriate base URL, as returned by <see cref="IServerApplicationHost.GetSmartApiUrl(HttpRequest)"/>.</param>
    /// <param name="localPath">The media source's local (LAN-access) path, as built from <see cref="IServerApplicationHost.GetApiUrlForLocalAccess"/>.</param>
    /// <param name="protocol">The media source's protocol.</param>
    /// <param name="baseUrl">The server's configured BaseUrl, if any.</param>
    /// <returns>The published path, or null if the local path should be left unchanged.</returns>
    internal static string? GetPublishedLiveStreamPath(
        string smartApiUrl,
        string? localPath,
        MediaProtocol protocol,
        string baseUrl)
    {
        if (protocol != MediaProtocol.Http
            || !Uri.TryCreate(localPath, UriKind.Absolute, out var localUri))
        {
            return null;
        }

        var relativePath = localUri.PathAndQuery;
        if (!string.IsNullOrEmpty(baseUrl))
        {
            var basePrefix = baseUrl + "/";
            if (!relativePath.StartsWith(basePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            relativePath = relativePath[baseUrl.Length..];
        }

        if (!relativePath.StartsWith("/LiveTv/LiveStreamFiles/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var prefix = smartApiUrl.TrimEnd('/');
        if (!string.IsNullOrEmpty(baseUrl))
        {
            var includesBaseUrl = Uri.TryCreate(prefix, UriKind.Absolute, out var publishedUri)
                && Uri.UnescapeDataString(publishedUri.AbsolutePath)
                    .TrimEnd('/')
                    .EndsWith(baseUrl, StringComparison.OrdinalIgnoreCase);

            if (!includesBaseUrl)
            {
                prefix += baseUrl;
            }
        }

        return prefix + relativePath;
    }
}
