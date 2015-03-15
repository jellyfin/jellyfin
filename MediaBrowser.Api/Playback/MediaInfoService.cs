using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.MediaInfo;
using ServiceStack;
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

        public async Task<object> Get(GetPlaybackInfo request)
        {
            var mediaSources = await _mediaSourceManager.GetPlayackMediaSources(request.Id, request.UserId, true, CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(new LiveMediaInfoResult
            {
                MediaSources = mediaSources.ToList()
            });
        }

        public async Task<object> Get(GetLiveMediaInfo request)
        {
            var mediaSources = await _mediaSourceManager.GetPlayackMediaSources(request.Id, request.UserId, true, CancellationToken.None).ConfigureAwait(false);

            return ToOptimizedResult(new LiveMediaInfoResult
            {
                MediaSources = mediaSources.ToList()
            });
        }
    }
}
