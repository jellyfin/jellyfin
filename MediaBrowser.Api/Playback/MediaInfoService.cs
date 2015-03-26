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

        [ApiMember(Name = "StartTimeTicks", Description = "Optional. Specify a starting offset, in ticks. 1 tick = 10000 ms", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public long? StartTimeTicks { get; set; }

        [ApiMember(Name = "AudioStreamIndex", Description = "Optional. The index of the audio stream to use. If omitted the first audio stream will be used.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? AudioStreamIndex { get; set; }

        [ApiMember(Name = "SubtitleStreamIndex", Description = "Optional. The index of the subtitle stream to use. If omitted no subtitles will be used.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? SubtitleStreamIndex { get; set; }
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
            var info = await GetPlaybackInfo(request.Id, request.UserId, request.MediaSource).ConfigureAwait(false);
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
                var mediaSourceId = request.MediaSource == null ? null : request.MediaSource.Id;
                SetDeviceSpecificData(request.Id, info, profile, authInfo, null, request.StartTimeTicks ?? 0, mediaSourceId, request.AudioStreamIndex, request.SubtitleStreamIndex);
            }

            return ToOptimizedResult(info);
        }

        private async Task<PlaybackInfoResponse> GetPlaybackInfo(string id, string userId, MediaSourceInfo mediaSource = null)
        {
            var result = new PlaybackInfoResponse();

            if (mediaSource == null)
            {
                IEnumerable<MediaSourceInfo> mediaSources;

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
            }
            else
            {
                result.MediaSources = new List<MediaSourceInfo> { mediaSource };
            }

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

        private void SetDeviceSpecificData(string itemId, 
            PlaybackInfoResponse result, 
            DeviceProfile profile, 
            AuthorizationInfo auth, 
            int? maxBitrate, 
            long startTimeTicks,
            string mediaSourceId,
            int? audioStreamIndex,
            int? subtitleStreamIndex)
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

                if (string.Equals(mediaSourceId, mediaSource.Id, StringComparison.OrdinalIgnoreCase))
                {
                    options.MediaSourceId = mediaSourceId;
                    options.AudioStreamIndex = audioStreamIndex;
                    options.SubtitleStreamIndex = subtitleStreamIndex;
                }

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
                        streamInfo.StartPositionTicks = startTimeTicks;
                        mediaSource.TranscodingUrl = streamInfo.ToUrl("-", auth.Token).Substring(1);
                        mediaSource.TranscodingContainer = streamInfo.Container;
                        mediaSource.TranscodingSubProtocol = streamInfo.SubProtocol;
                    }
                }
            }
        }
    }
}
