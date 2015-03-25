using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Playback
{
    [Route("/Items/{Id}/MediaInfo", "GET", Summary = "Gets live playback media info for an item")]
    public class GetLiveMediaInfo : IReturn<LiveMediaInfoResult>
    {
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/Items/{Id}/PlaybackInfo", "GET", Summary = "Gets live playback media info for an item")]
    public class GetPlaybackInfo : IReturn<LiveMediaInfoResult>
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

        public MediaInfoService(IMediaSourceManager mediaSourceManager)
        {
            _mediaSourceManager = mediaSourceManager;
        }

        public Task<object> Get(GetPlaybackInfo request)
        {
            return GetPlaybackInfo(request.Id, request.UserId);
        }

        public Task<object> Get(GetLiveMediaInfo request)
        {
            return GetPlaybackInfo(request.Id, request.UserId);
        }

        private async Task<object> GetPlaybackInfo(string id, string userId)
        {
            IEnumerable<MediaSourceInfo> mediaSources;
            var result = new LiveMediaInfoResult();

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
            result.StreamId = Guid.NewGuid().ToString("N");

            return ToOptimizedResult(result);
        }
    }
}
