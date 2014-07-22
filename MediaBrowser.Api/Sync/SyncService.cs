using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using ServiceStack;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Sync
{
    [Route("/Sync/Jobs/{Id}", "DELETE", Summary = "Cancels a sync job.")]
    public class CancelSyncJob : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Sync/Schedules/{Id}", "DELETE", Summary = "Cancels a sync job.")]
    public class CancelSyncSchedule : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Sync/Jobs/{Id}", "GET", Summary = "Gets a sync job.")]
    public class GetSyncJob : IReturn<SyncJob>
    {
        [ApiMember(Name = "Id", Description = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Sync/Schedules/{Id}", "GET", Summary = "Gets a sync job.")]
    public class GetSyncSchedule : IReturn<SyncSchedule>
    {
        [ApiMember(Name = "Id", Description = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Sync/Jobs", "GET", Summary = "Gets sync jobs.")]
    public class GetSyncJobs : IReturn<QueryResult<SyncJob>>
    {
    }

    [Route("/Sync/Schedules", "GET", Summary = "Gets sync schedules.")]
    public class GetSyncSchedules : IReturn<QueryResult<SyncSchedule>>
    {
    }

    [Route("/Sync/Jobs", "POST", Summary = "Gets sync jobs.")]
    public class CreateSyncJob : SyncJobRequest
    {
    }

    [Route("/Sync/Targets", "GET", Summary = "Gets a list of available sync targets.")]
    public class GetSyncTarget : IReturn<List<SyncTarget>>
    {
        [ApiMember(Name = "UserId", Description = "UserId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Authenticated]
    public class SyncService : BaseApiService
    {
        private readonly ISyncManager _syncManager;

        public SyncService(ISyncManager syncManager)
        {
            _syncManager = syncManager;
        }

        public object Get(GetSyncTarget request)
        {
            var result = _syncManager.GetSyncTargets(request.UserId);

            return ToOptimizedResult(result);
        }

        public object Get(GetSyncJobs request)
        {
            var result = _syncManager.GetJobs(new SyncJobQuery
            {
                 
            });

            return ToOptimizedResult(result);
        }

        public object Get(GetSyncSchedules request)
        {
            var result = _syncManager.GetSchedules(new SyncScheduleQuery
            {
                 
            }); 

            return ToOptimizedResult(result);
        }

        public object Get(GetSyncJob request)
        {
            var result = _syncManager.GetJob(request.Id);

            return ToOptimizedResult(result);
        }

        public object Get(GetSyncSchedule request)
        {
            var result = _syncManager.GetSchedule(request.Id);

            return ToOptimizedResult(result);
        }

        public void Delete(CancelSyncJob request)
        {
            var task = _syncManager.CancelJob(request.Id);

            Task.WaitAll(task);
        }

        public void Delete(CancelSyncSchedule request)
        {
            var task = _syncManager.CancelSchedule(request.Id);

            Task.WaitAll(task);
        }

        public void Post(CreateSyncJob request)
        {
            var task = _syncManager.CreateJob(request);

            Task.WaitAll(task);
        }
    }
}
