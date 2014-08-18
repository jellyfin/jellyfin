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

    [Route("/Sync/Jobs/{Id}", "GET", Summary = "Gets a sync job.")]
    public class GetSyncJob : IReturn<SyncJob>
    {
        [ApiMember(Name = "Id", Description = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Sync/Jobs", "GET", Summary = "Gets sync jobs.")]
    public class GetSyncJobs : IReturn<QueryResult<SyncJob>>
    {
        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }
    }

    [Route("/Sync/Jobs", "POST", Summary = "Gets sync jobs.")]
    public class CreateSyncJob : SyncJobRequest, IReturn<SyncJobCreationResult>
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
                 StartIndex = request.StartIndex,
                 Limit = request.Limit
            });

            return ToOptimizedResult(result);
        }

        public object Get(GetSyncJob request)
        {
            var result = _syncManager.GetJob(request.Id);

            return ToOptimizedResult(result);
        }

        public void Delete(CancelSyncJob request)
        {
            var task = _syncManager.CancelJob(request.Id);

            Task.WaitAll(task);
        }

        public async Task<object> Post(CreateSyncJob request)
        {
            var result = await _syncManager.CreateJob(request).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }
    }
}
