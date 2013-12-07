using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System;
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
    public class GetChannels : IReturn<QueryResult<ChannelInfoDto>>
    {
        [ApiMember(Name = "Type", Description = "Optional filter by channel type.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public ChannelType? Type { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional filter by user id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/LiveTv/Channels/{Id}", "GET")]
    [Api(Description = "Gets a live tv channel")]
    public class GetChannel : IReturn<ChannelInfoDto>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Channel Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional user id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/LiveTv/Recordings", "GET")]
    [Api(Description = "Gets live tv recordings")]
    public class GetRecordings : IReturn<QueryResult<RecordingInfoDto>>
    {
        [ApiMember(Name = "ChannelId", Description = "Optional filter by channel id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ChannelId { get; set; }
    }

    [Route("/LiveTv/Recordings/{Id}", "GET")]
    [Api(Description = "Gets a live tv recording")]
    public class GetRecording : IReturn<RecordingInfoDto>
    {
        [ApiMember(Name = "Id", Description = "Recording Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/Timers/{Id}", "GET")]
    [Api(Description = "Gets a live tv timer")]
    public class GetTimer : IReturn<TimerInfoDto>
    {
        [ApiMember(Name = "Id", Description = "Timer Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/Timers", "GET")]
    [Api(Description = "Gets live tv timers")]
    public class GetTimers : IReturn<QueryResult<TimerInfoDto>>
    {
        [ApiMember(Name = "ChannelId", Description = "Optional filter by channel id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ChannelId { get; set; }
    }

    [Route("/LiveTv/Programs", "GET")]
    [Api(Description = "Gets available live tv epgs..")]
    public class GetPrograms : IReturn<QueryResult<ProgramInfoDto>>
    {
        [ApiMember(Name = "ChannelIds", Description = "The channels to return guide information for.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ChannelIds { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional filter by user id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/LiveTv/Recordings/{Id}", "DELETE")]
    [Api(Description = "Deletes a live tv recording")]
    public class DeleteRecording : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Recording Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/Timers/{Id}", "DELETE")]
    [Api(Description = "Cancels a live tv timer")]
    public class CancelTimer : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Timer Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
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
            var services = _liveTvManager.Services
                .Select(GetServiceInfo)
                .ToList();

            return ToOptimizedResult(services);
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
            var result = _liveTvManager.GetChannels(new ChannelQuery
            {
                ChannelType = request.Type,
                UserId = request.UserId

            });

            return ToOptimizedResult(result);
        }

        public object Get(GetChannel request)
        {
            var result = _liveTvManager.GetChannelInfoDto(request.Id, request.UserId);

            return ToOptimizedResult(result);
        }

        public object Get(GetPrograms request)
        {
            var result = _liveTvManager.GetPrograms(new ProgramQuery
            {
                ChannelIdList = (request.ChannelIds ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToArray(),
                UserId = request.UserId

            }, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }

        public object Get(GetRecordings request)
        {
            var result = _liveTvManager.GetRecordings(new RecordingQuery
            {
                ChannelId = request.ChannelId

            }, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }

        public object Get(GetRecording request)
        {
            var result = _liveTvManager.GetRecording(request.Id, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }

        public object Get(GetTimer request)
        {
            var result = _liveTvManager.GetTimer(request.Id, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }

        public object Get(GetTimers request)
        {
            var result = _liveTvManager.GetTimers(new TimerQuery
            {
                ChannelId = request.ChannelId

            }, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }

        public void Delete(DeleteRecording request)
        {
            var task = _liveTvManager.DeleteRecording(request.Id);

            Task.WaitAll(task);
        }

        public void Delete(CancelTimer request)
        {
            var task = _liveTvManager.CancelTimer(request.Id);

            Task.WaitAll(task);
        }
    }
}