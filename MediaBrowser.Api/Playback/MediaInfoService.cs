using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Session;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback
{
    [Route("/Items/{Id}/MediaInfo", "GET", Summary = "Gets live playback media info for an item")]
    public class GetLiveMediaInfo : IReturn<PlaybackInfoResponse>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/Items/{Id}/PlaybackInfo", "GET", Summary = "Gets live playback media info for an item")]
    public class GetPlaybackInfo : IReturn<PlaybackInfoResponse>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/Items/{Id}/PlaybackInfo", "POST", Summary = "Gets live playback media info for an item")]
    public class GetPostedPlaybackInfo : PlaybackInfoRequest, IReturn<PlaybackInfoResponse>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Authenticated]
    public class MediaInfoService : BaseApiService
    {
        private readonly IMediaSourceManager _mediaSourceManager;
        private readonly IDeviceManager _deviceManager;
        private readonly ILibraryManager _libraryManager;

        public MediaInfoService(IMediaSourceManager mediaSourceManager, IDeviceManager deviceManager, ILibraryManager libraryManager)
        {
            _mediaSourceManager = mediaSourceManager;
            _deviceManager = deviceManager;
            _libraryManager = libraryManager;
        }

        public async Task<object> Get(GetPlaybackInfo request)
        {
            var result = await GetPlaybackInfo(request.Id, request.UserId).ConfigureAwait(false);
            return ToOptimizedResult(result);
        }

        public async Task<object> Get(GetLiveMediaInfo request)
        {
            var result = await GetPlaybackInfo(request.Id, request.UserId).ConfigureAwait(false);
            return ToOptimizedResult(result);
        }

        public async Task<object> Post(GetPostedPlaybackInfo request)
        {
            var info = await GetPlaybackInfo(request.Id, request.UserId).ConfigureAwait(false);
            var authInfo = AuthorizationContext.GetAuthorizationInfo(Request);

            var profile = request.DeviceProfile;
            //if (profile == null)
            //{
            //    var caps = _deviceManager.GetCapabilities(authInfo.DeviceId);
            //    if (caps != null)
            //    {
            //        profile = caps.DeviceProfile;
            //    }
            //}

            if (profile != null)
            {
                SetDeviceSpecificData(request.Id, info, profile, authInfo, null);
            }

            return ToOptimizedResult(info);
        }

        private async Task<PlaybackInfoResponse> GetPlaybackInfo(string id, string userId)
        {
            IEnumerable<MediaSourceInfo> mediaSources;
            var result = new PlaybackInfoResponse();

            try
            {
                mediaSources = await _mediaSourceManager.GetPlayackMediaSources(id, userId, true, CancellationToken.None).ConfigureAwait(false);
            }
            catch (PlaybackException ex)
            {
                mediaSources = new List<MediaSourceInfo>();
                result.ErrorCode = ex.ErrorCode;
            }

            result.MediaSources = mediaSources.ToList();

            if (result.MediaSources.Count == 0)
            {
                if (!result.ErrorCode.HasValue)
                {
                    result.ErrorCode = PlaybackErrorCode.NoCompatibleStream;
                }
            }
            else
            {
                result.StreamId = Guid.NewGuid().ToString("N");
            }

            return result;
        }

        private void SetDeviceSpecificData(string itemId, PlaybackInfoResponse result, DeviceProfile profile, AuthorizationInfo auth, int? maxBitrate)
        {
            var streamBuilder = new StreamBuilder();

            var item = _libraryManager.GetItemById(itemId);

            foreach (var mediaSource in result.MediaSources)
            {
                var options = new VideoOptions
                {
                    MediaSources = new List<MediaSourceInfo> { mediaSource },
                    Context = EncodingContext.Streaming,
                    DeviceId = auth.DeviceId,
                    ItemId = item.Id.ToString("N"),
                    Profile = profile,
                    MaxBitrate = maxBitrate
                };

                if (mediaSource.SupportsDirectPlay)
                {
                    var supportsDirectStream = mediaSource.SupportsDirectStream;

                    // Dummy this up to fool StreamBuilder
                    mediaSource.SupportsDirectStream = true;
                    
                    // The MediaSource supports direct stream, now test to see if the client supports it
                    var streamInfo = item is Video ?
                        streamBuilder.BuildVideoItem(options) :
                        streamBuilder.BuildAudioItem(options);

                    if (streamInfo == null || !streamInfo.IsDirectStream)
                    {
                        mediaSource.SupportsDirectPlay = false;
                    }

                    // Set this back to what it was
                    mediaSource.SupportsDirectStream = supportsDirectStream;
                }

                if (mediaSource.SupportsDirectStream)
                {
                    // The MediaSource supports direct stream, now test to see if the client supports it
                    var streamInfo = item is Video ?
                        streamBuilder.BuildVideoItem(options) :
                        streamBuilder.BuildAudioItem(options);

                    if (streamInfo == null || !streamInfo.IsDirectStream)
                    {
                        mediaSource.SupportsDirectStream = false;
                    }
                }

                if (mediaSource.SupportsTranscoding)
                {
                    // The MediaSource supports direct stream, now test to see if the client supports it
                    var streamInfo = item is Video ?
                        streamBuilder.BuildVideoItem(options) :
                        streamBuilder.BuildAudioItem(options);

                    if (streamInfo != null && streamInfo.PlayMethod == PlayMethod.Transcode)
                    {
                        mediaSource.TranscodingUrl = streamInfo.ToUrl("-", auth.Token).Substring(1);
                        mediaSource.TranscodingContainer = streamInfo.Container;
                        mediaSource.TranscodingSubProtocol = streamInfo.SubProtocol;
                    }
                }
            }
        }
    }
}
