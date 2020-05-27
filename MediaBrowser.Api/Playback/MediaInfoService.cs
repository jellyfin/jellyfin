#pragma warning disable CS1591
#pragma warning disable SA1402
#pragma warning disable SA1649

using System;
using System.Buffers;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Playback
{
    [Route("/Items/{Id}/PlaybackInfo", "GET", Summary = "Gets live playback media info for an item")]
    public class GetPlaybackInfo : IReturn<PlaybackInfoResponse>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public Guid Id { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }
    }

    [Route("/Items/{Id}/PlaybackInfo", "POST", Summary = "Gets live playback media info for an item")]
    public class GetPostedPlaybackInfo : PlaybackInfoRequest, IReturn<PlaybackInfoResponse>
    {
    }

    [Route("/LiveStreams/Open", "POST", Summary = "Opens a media source")]
    public class OpenMediaSource : LiveStreamRequest, IReturn<LiveStreamResponse>
    {
    }

    [Route("/LiveStreams/Close", "POST", Summary = "Closes a media source")]
    public class CloseMediaSource : IReturnVoid
    {
        [ApiMember(Name = "LiveStreamId", Description = "LiveStreamId", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string LiveStreamId { get; set; }
    }

    [Route("/Playback/BitrateTest", "GET")]
    public class GetBitrateTestBytes
    {
        [ApiMember(Name = "Size", Description = "Size", IsRequired = true, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int Size { get; set; }

        public GetBitrateTestBytes()
        {
            // 100k
            Size = 102400;
        }
    }

    [Authenticated]
    public class MediaInfoService : BaseApiService
    {
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IDeviceManager _deviceManager;
        private readonly ILibraryManager _libraryManager;
        private readonly INetworkManager _networkManager;
        private readonly IMediaEncoder _mediaEncoder;
        private readonly IUserManager _userManager;
        private readonly IAuthorizationContext _authContext;

        public MediaInfoService(
            ILogger<MediaInfoService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IMediaSourceManager mediaSourceManager,
            IDeviceManager deviceManager,
            ILibraryManager libraryManager,
            INetworkManager networkManager,
            IMediaEncoder mediaEncoder,
            IUserManager userManager,
            IAuthorizationContext authContext)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _mediaSourceManager = mediaSourceManager;
            _deviceManager = deviceManager;
            _libraryManager = libraryManager;
            _networkManager = networkManager;
            _mediaEncoder = mediaEncoder;
            _userManager = userManager;
            _authContext = authContext;
        }

        public object Get(GetBitrateTestBytes request)
        {
            const int MaxSize = 10_000_000;

            var size = request.Size;

            if (size <= 0)
            {
                throw new ArgumentException($"The requested size ({size}) is equal to or smaller than 0.", nameof(request));
            }

            if (size > MaxSize)
            {
                throw new ArgumentException($"The requested size ({size}) is larger than the max allowed value ({MaxSize}).", nameof(request));
            }

            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                new Random().NextBytes(buffer);
                return ResultFactory.GetResult(null, buffer, "application/octet-stream");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public async Task<object> Get(GetPlaybackInfo request)
        {
            var result = await GetPlaybackInfo(request.Id, request.UserId, new[] { MediaType.Audio, MediaType.Video }).ConfigureAwait(false);
            return ToOptimizedResult(result);
        }

        public async Task<object> Post(OpenMediaSource request)
        {
            var result = await OpenMediaSource(request).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        private async Task<LiveStreamResponse> OpenMediaSource(OpenMediaSource request)
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

                SetDeviceSpecificData(item, result.MediaSource, profile, authInfo, request.MaxStreamingBitrate,
                    request.StartTimeTicks ?? 0, result.MediaSource.Id, request.AudioStreamIndex,
                    request.SubtitleStreamIndex, request.MaxAudioChannels, request.PlaySessionId, request.UserId, request.EnableDirectPlay, true, request.EnableDirectStream, true, true, true);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(result.MediaSource.TranscodingUrl))
                {
                    result.MediaSource.TranscodingUrl += "&LiveStreamId=" + result.MediaSource.LiveStreamId;
                }
            }

            if (result.MediaSource != null)
            {
                NormalizeMediaSourceContainer(result.MediaSource, profile, DlnaProfileType.Video);
            }

            return result;
        }

        public void Post(CloseMediaSource request)
        {
            _mediaSourceManager.CloseLiveStream(request.LiveStreamId).GetAwaiter().GetResult();
        }

        public async Task<PlaybackInfoResponse> GetPlaybackInfo(GetPostedPlaybackInfo request)
        {
            var authInfo = _authContext.GetAuthorizationInfo(Request);

            var profile = request.DeviceProfile;

            Logger.LogInformation("GetPostedPlaybackInfo profile: {@Profile}", profile);

            if (profile == null)
            {
                var caps = _deviceManager.GetCapabilities(authInfo.DeviceId);
                if (caps != null)
                {
                    profile = caps.DeviceProfile;
                }
            }

            var info = await GetPlaybackInfo(request.Id, request.UserId, new[] { MediaType.Audio, MediaType.Video }, request.MediaSourceId, request.LiveStreamId).ConfigureAwait(false);

            if (profile != null)
            {
                var mediaSourceId = request.MediaSourceId;

                SetDeviceSpecificData(request.Id, info, profile, authInfo, request.MaxStreamingBitrate ?? profile.MaxStreamingBitrate, request.StartTimeTicks ?? 0, mediaSourceId, request.AudioStreamIndex, request.SubtitleStreamIndex, request.MaxAudioChannels, request.UserId, request.EnableDirectPlay, true, request.EnableDirectStream, request.EnableTranscoding, request.AllowVideoStreamCopy, request.AllowAudioStreamCopy);
            }

            if (request.AutoOpenLiveStream)
            {
                var mediaSource = string.IsNullOrWhiteSpace(request.MediaSourceId) ? info.MediaSources.FirstOrDefault() : info.MediaSources.FirstOrDefault(i => string.Equals(i.Id, request.MediaSourceId, StringComparison.Ordinal));

                if (mediaSource != null && mediaSource.RequiresOpening && string.IsNullOrWhiteSpace(mediaSource.LiveStreamId))
                {
                    var openStreamResult = await OpenMediaSource(new OpenMediaSource
                    {
                        AudioStreamIndex = request.AudioStreamIndex,
                        DeviceProfile = request.DeviceProfile,
                        EnableDirectPlay = request.EnableDirectPlay,
                        EnableDirectStream = request.EnableDirectStream,
                        ItemId = request.Id,
                        MaxAudioChannels = request.MaxAudioChannels,
                        MaxStreamingBitrate = request.MaxStreamingBitrate,
                        PlaySessionId = info.PlaySessionId,
                        StartTimeTicks = request.StartTimeTicks,
                        SubtitleStreamIndex = request.SubtitleStreamIndex,
                        UserId = request.UserId,
                        OpenToken = mediaSource.OpenToken
                    }).ConfigureAwait(false);

                    info.MediaSources = new[] { openStreamResult.MediaSource };
                }
            }

            if (info.MediaSources != null)
            {
                foreach (var mediaSource in info.MediaSources)
                {
                    NormalizeMediaSourceContainer(mediaSource, profile, DlnaProfileType.Video);
                }
            }

            return info;
        }

        private void NormalizeMediaSourceContainer(MediaSourceInfo mediaSource, DeviceProfile profile, DlnaProfileType type)
        {
            mediaSource.Container = StreamBuilder.NormalizeMediaSourceFormatIntoSingleContainer(mediaSource.Container, mediaSource.Path, profile, type);
        }

        public async Task<object> Post(GetPostedPlaybackInfo request)
        {
            var result = await GetPlaybackInfo(request).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        private async Task<PlaybackInfoResponse> GetPlaybackInfo(Guid id, Guid userId, string[] supportedLiveMediaTypes, string mediaSourceId = null, string liveStreamId = null)
        {
            var user = _userManager.GetUserById(userId);
            var item = _libraryManager.GetItemById(id);
            var result = new PlaybackInfoResponse();

            MediaSourceInfo[] mediaSources;
            if (string.IsNullOrWhiteSpace(liveStreamId))
            {

                // TODO handle supportedLiveMediaTypes?
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

                if (!result.ErrorCode.HasValue)
                {
                    result.ErrorCode = PlaybackErrorCode.NoCompatibleStream;
                }
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

        private void SetDeviceSpecificData(
            Guid itemId,
            PlaybackInfoResponse result,
            DeviceProfile profile,
            AuthorizationInfo auth,
            long? maxBitrate,
            long startTimeTicks,
            string mediaSourceId,
            int? audioStreamIndex,
            int? subtitleStreamIndex,
            int? maxAudioChannels,
            Guid userId,
            bool enableDirectPlay,
            bool forceDirectPlayRemoteMediaSource,
            bool enableDirectStream,
            bool enableTranscoding,
            bool allowVideoStreamCopy,
            bool allowAudioStreamCopy)
        {
            var item = _libraryManager.GetItemById(itemId);

            foreach (var mediaSource in result.MediaSources)
            {
                SetDeviceSpecificData(item, mediaSource, profile, auth, maxBitrate, startTimeTicks, mediaSourceId, audioStreamIndex, subtitleStreamIndex, maxAudioChannels, result.PlaySessionId, userId, enableDirectPlay, forceDirectPlayRemoteMediaSource, enableDirectStream, enableTranscoding, allowVideoStreamCopy, allowAudioStreamCopy);
            }

            SortMediaSources(result, maxBitrate);
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
            bool forceDirectPlayRemoteMediaSource,
            bool enableDirectStream,
            bool enableTranscoding,
            bool allowVideoStreamCopy,
            bool allowAudioStreamCopy)
        {
            var streamBuilder = new StreamBuilder(_mediaEncoder, Logger);

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
                Logger.LogInformation("User policy for {0}. EnableAudioPlaybackTranscoding: {1}", user.Name, user.Policy.EnableAudioPlaybackTranscoding);
            }
            else
            {
                Logger.LogInformation("User policy for {0}. EnablePlaybackRemuxing: {1} EnableVideoPlaybackTranscoding: {2} EnableAudioPlaybackTranscoding: {3}",
                    user.Name,
                    user.Policy.EnablePlaybackRemuxing,
                    user.Policy.EnableVideoPlaybackTranscoding,
                    user.Policy.EnableAudioPlaybackTranscoding);
            }

            // Beginning of Playback Determination: Attempt DirectPlay first
            if (mediaSource.SupportsDirectPlay)
            {
                if (mediaSource.IsRemote && user.Policy.ForceRemoteSourceTranscoding)
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
                        if (!user.Policy.EnableAudioPlaybackTranscoding)
                        {
                            options.ForceDirectPlay = true;
                        }
                    }
                    else if (item is Video)
                    {
                        if (!user.Policy.EnableAudioPlaybackTranscoding && !user.Policy.EnableVideoPlaybackTranscoding && !user.Policy.EnablePlaybackRemuxing)
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
                if (mediaSource.IsRemote && user.Policy.ForceRemoteSourceTranscoding)
                {
                    mediaSource.SupportsDirectStream = false;
                }
                else
                {
                    options.MaxBitrate = GetMaxBitrate(maxBitrate, user);

                    if (item is Audio)
                    {
                        if (!user.Policy.EnableAudioPlaybackTranscoding)
                        {
                            options.ForceDirectStream = true;
                        }
                    }
                    else if (item is Video)
                    {
                        if (!user.Policy.EnableAudioPlaybackTranscoding && !user.Policy.EnableVideoPlaybackTranscoding && !user.Policy.EnablePlaybackRemuxing)
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

                if (mediaSource.IsRemote && user.Policy.ForceRemoteSourceTranscoding)
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

        private long? GetMaxBitrate(long? clientMaxBitrate, User user)
        {
            var maxBitrate = clientMaxBitrate;
            var remoteClientMaxBitrate = user?.Policy.RemoteClientBitrateLimit ?? 0;

            if (remoteClientMaxBitrate <= 0)
            {
                remoteClientMaxBitrate = ServerConfigurationManager.Configuration.RemoteClientBitrateLimit;
            }

            if (remoteClientMaxBitrate > 0)
            {
                var isInLocalNetwork = _networkManager.IsInLocalNetwork(Request.RemoteIp);

                Logger.LogInformation("RemoteClientBitrateLimit: {0}, RemoteIp: {1}, IsInLocalNetwork: {2}", remoteClientMaxBitrate, Request.RemoteIp, isInLocalNetwork);
                if (!isInLocalNetwork)
                {
                    maxBitrate = Math.Min(maxBitrate ?? remoteClientMaxBitrate, remoteClientMaxBitrate);
                }
            }

            return maxBitrate;
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

            }).ThenBy(i =>
            {
                // Let's assume direct streaming a file is just as desirable as direct playing a remote url
                if (i.SupportsDirectPlay || i.SupportsDirectStream)
                {
                    return 0;
                }

                return 1;

            }).ThenBy(i =>
            {
                return i.Protocol switch
                {
                    MediaProtocol.File => 0,
                    _ => 1,
                };
            }).ThenBy(i =>
            {
                if (maxBitrate.HasValue && i.Bitrate.HasValue)
                {
                    return i.Bitrate.Value <= maxBitrate.Value ? 0 : 2;
                }

                return 1;

            }).ThenBy(originalList.IndexOf)
            .ToArray();
        }
    }
}
