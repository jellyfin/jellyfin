using MediaBrowser.Controller.Channels;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System.Threading;

namespace MediaBrowser.Api
{
    [Route("/Channels", "GET")]
    [Api(("Gets available channels"))]
    public class GetChannels : IReturn<QueryResult<BaseItemDto>>
    {
        public string UserId { get; set; }

        public int? StartIndex { get; set; }

        public int? Limit { get; set; }
    }

    public class ChannelService : BaseApiService
    {
        private readonly IChannelManager _channelManager;

        public ChannelService(IChannelManager channelManager)
        {
            _channelManager = channelManager;
        }

        public object Get(GetChannels request)
        {
            var result = _channelManager.GetChannels(new ChannelQuery
            {
                Limit = request.Limit,
                StartIndex = request.StartIndex,
                UserId = request.UserId,

            }, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }
    }
}
