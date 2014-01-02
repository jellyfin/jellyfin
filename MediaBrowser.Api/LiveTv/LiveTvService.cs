using MediaBrowser.Controller.Library;
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

        [ApiMember(Name = "UserId", Description = "Optional filter by user and attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
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

        [ApiMember(Name = "UserId", Description = "Optional attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/LiveTv/Recordings", "GET")]
    [Api(Description = "Gets live tv recordings")]
    public class GetRecordings : IReturn<QueryResult<RecordingInfoDto>>
    {
        [ApiMember(Name = "ChannelId", Description = "Optional filter by channel id.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ChannelId { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional filter by user and attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

        [ApiMember(Name = "GroupId", Description = "Optional filter by recording group.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string GroupId { get; set; }

        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }

        [ApiMember(Name = "IsRecording", Description = "Optional filter by recordings that are currently active, or not.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool? IsRecording { get; set; }
    }

    [Route("/LiveTv/Recordings/Groups", "GET")]
    [Api(Description = "Gets live tv recording groups")]
    public class GetRecordingGroups : IReturn<QueryResult<RecordingGroupDto>>
    {
        [ApiMember(Name = "UserId", Description = "Optional filter by user and attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/LiveTv/Recordings/{Id}", "GET")]
    [Api(Description = "Gets a live tv recording")]
    public class GetRecording : IReturn<RecordingInfoDto>
    {
        [ApiMember(Name = "Id", Description = "Recording Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/LiveTv/Timers/{Id}", "GET")]
    [Api(Description = "Gets a live tv timer")]
    public class GetTimer : IReturn<TimerInfoDto>
    {
        [ApiMember(Name = "Id", Description = "Timer Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/Timers/Defaults", "GET")]
    [Api(Description = "Gets default values for a new timer")]
    public class GetDefaultTimer : IReturn<SeriesTimerInfoDto>
    {
        [ApiMember(Name = "ProgramId", Description = "Optional, to attach default values based on a program.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ProgramId { get; set; }
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

    [Route("/LiveTv/Programs/{Id}", "GET")]
    [Api(Description = "Gets a live tv program")]
    public class GetProgram : IReturn<ProgramInfoDto>
    {
        [ApiMember(Name = "Id", Description = "Program Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "UserId", Description = "Optional attach user data.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
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

    [Route("/LiveTv/Timers/{Id}", "POST")]
    [Api(Description = "Updates a live tv timer")]
    public class UpdateTimer : TimerInfoDto, IReturnVoid
    {
    }

    [Route("/LiveTv/Timers", "POST")]
    [Api(Description = "Creates a live tv timer")]
    public class CreateTimer : TimerInfoDto, IReturnVoid
    {
    }

    [Route("/LiveTv/SeriesTimers/{Id}", "GET")]
    [Api(Description = "Gets a live tv series timer")]
    public class GetSeriesTimer : IReturn<TimerInfoDto>
    {
        [ApiMember(Name = "Id", Description = "Timer Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/SeriesTimers", "GET")]
    [Api(Description = "Gets live tv series timers")]
    public class GetSeriesTimers : IReturn<QueryResult<SeriesTimerInfoDto>>
    {
    }

    [Route("/LiveTv/SeriesTimers/{Id}", "DELETE")]
    [Api(Description = "Cancels a live tv series timer")]
    public class CancelSeriesTimer : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Timer Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/LiveTv/SeriesTimers/{Id}", "POST")]
    [Api(Description = "Updates a live tv series timer")]
    public class UpdateSeriesTimer : SeriesTimerInfoDto, IReturnVoid
    {
    }

    [Route("/LiveTv/SeriesTimers", "POST")]
    [Api(Description = "Creates a live tv series timer")]
    public class CreateSeriesTimer : SeriesTimerInfoDto, IReturnVoid
    {
    }

    [Route("/LiveTv/Recordings/Groups/{Id}", "GET")]
    [Api(Description = "Gets a recording group")]
    public class GetRecordingGroup : IReturn<RecordingGroupDto>
    {
        [ApiMember(Name = "Id", Description = "Recording group Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    public class LiveTvService : BaseApiService
    {
        private readonly ILiveTvManager _liveTvManager;
        private readonly IUserManager _userManager;

        public LiveTvService(ILiveTvManager liveTvManager, IUserManager userManager)
        {
            _liveTvManager = liveTvManager;
            _userManager = userManager;
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

            }, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }

        public object Get(GetChannel request)
        {
            var user = string.IsNullOrEmpty(request.UserId) ? null : _userManager.GetUserById(new Guid(request.UserId));

            var result = _liveTvManager.GetChannel(request.Id, CancellationToken.None, user).Result;

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
                ChannelId = request.ChannelId,
                UserId = request.UserId,
                GroupId = request.GroupId,
                StartIndex = request.StartIndex,
                Limit = request.Limit,
                IsRecording = request.IsRecording

            }, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }

        public object Get(GetRecording request)
        {
            var user = string.IsNullOrEmpty(request.UserId) ? null : _userManager.GetUserById(new Guid(request.UserId));

            var result = _liveTvManager.GetRecording(request.Id, CancellationToken.None, user).Result;

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

        public void Post(UpdateTimer request)
        {
            var task = _liveTvManager.UpdateTimer(request, CancellationToken.None);

            Task.WaitAll(task);
        }

        public object Get(GetSeriesTimers request)
        {
            var result = _liveTvManager.GetSeriesTimers(new SeriesTimerQuery
            {

            }, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }

        public object Get(GetSeriesTimer request)
        {
            var result = _liveTvManager.GetSeriesTimer(request.Id, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }

        public void Delete(CancelSeriesTimer request)
        {
            var task = _liveTvManager.CancelSeriesTimer(request.Id);

            Task.WaitAll(task);
        }

        public void Post(UpdateSeriesTimer request)
        {
            var task = _liveTvManager.UpdateSeriesTimer(request, CancellationToken.None);

            Task.WaitAll(task);
        }

        public object Get(GetDefaultTimer request)
        {
            if (string.IsNullOrEmpty(request.ProgramId))
            {
                var result = _liveTvManager.GetNewTimerDefaults(CancellationToken.None).Result;

                return ToOptimizedResult(result);
            }
            else
            {
                var result = _liveTvManager.GetNewTimerDefaults(request.ProgramId, CancellationToken.None).Result;

                return ToOptimizedResult(result);
            }
        }

        public object Get(GetProgram request)
        {
            var user = string.IsNullOrEmpty(request.UserId) ? null : _userManager.GetUserById(new Guid(request.UserId));

            var result = _liveTvManager.GetProgram(request.Id, CancellationToken.None, user).Result;

            return ToOptimizedResult(result);
        }

        public void Post(CreateSeriesTimer request)
        {
            var task = _liveTvManager.CreateSeriesTimer(request, CancellationToken.None);

            Task.WaitAll(task);
        }

        public void Post(CreateTimer request)
        {
            var task = _liveTvManager.CreateTimer(request, CancellationToken.None);

            Task.WaitAll(task);
        }

        public object Get(GetRecordingGroups request)
        {
            var result = _liveTvManager.GetRecordingGroups(new RecordingGroupQuery
            {
                UserId = request.UserId

            }, CancellationToken.None).Result;

            return ToOptimizedResult(result);
        }

        public object Get(GetRecordingGroup request)
        {
            var result = _liveTvManager.GetRecordingGroups(new RecordingGroupQuery
            {

            }, CancellationToken.None).Result;

            var group = result.Items.FirstOrDefault(i => string.Equals(i.Id, request.Id, StringComparison.OrdinalIgnoreCase));

            return ToOptimizedResult(group);
        }
    }
}