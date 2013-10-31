using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using ServiceStack.ServiceHost;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api.LiveTv
{
    [Route("/LiveTv/Services", "GET")]
    [Api(Description = "Gets available live tv services.")]
    public class GetServices : IReturn<List<LiveTvServiceInfo>>
    {
    }

    [Route("/LiveTv/Channels", "GET")]
    [Api(Description = "Gets available live tv channels.")]
    public class GetChannels : IReturn<List<ChannelInfoDto>>
    {
        // Add filter by service if needed, and/or other filters
    }

    [Route("/LiveTv/Recordings", "GET")]
    [Api(Description = "Gets available live tv recordings.")]
    public class GetRecordings : IReturn<List<RecordingInfo>>
    {
        // Add filter by service if needed, and/or other filters
    }
    
    public class LiveTvService : BaseApiService
    {
        private readonly ILiveTvManager _liveTvManager;

        public LiveTvService(ILiveTvManager liveTvManager)
        {
            _liveTvManager = liveTvManager;
        }

        public object Get(GetServices request)
        {
            var services = _liveTvManager.Services;

            var result = services.Select(GetServiceInfo)
                .ToList();

            return ToOptimizedResult(result);
        }

        private LiveTvServiceInfo GetServiceInfo(ILiveTvService service)
        {
            return new LiveTvServiceInfo
            {
                Name = service.Name
            };
        }

        public object Get(GetChannels request)
        {
            var result = GetChannelsAsync(request).Result;

            return ToOptimizedResult(result);
        }

        private async Task<IEnumerable<ChannelInfoDto>> GetChannelsAsync(GetChannels request)
        {
            var services = _liveTvManager.Services;

            var tasks = services.Select(i => i.GetChannelsAsync(CancellationToken.None));

            var channelLists = await Task.WhenAll(tasks).ConfigureAwait(false);

            // Aggregate all channels from all services
            return channelLists.SelectMany(i => i)
                .Select(_liveTvManager.GetChannelInfoDto);
        }

        public object Get(GetRecordings request)
        {
            var result = GetRecordingsAsync(request).Result;

            return ToOptimizedResult(result);
        }

        private async Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(GetRecordings request)
        {
            var services = _liveTvManager.Services;

            var tasks = services.Select(i => i.GetRecordingsAsync(CancellationToken.None));

            var recordings = await Task.WhenAll(tasks).ConfigureAwait(false);

            return recordings.SelectMany(i => i);
        }
    }
}
