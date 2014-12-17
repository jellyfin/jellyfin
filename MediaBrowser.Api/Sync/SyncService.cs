using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using ServiceStack;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class GetSyncTargets : IReturn<List<SyncTarget>>
    {
        [ApiMember(Name = "UserId", Description = "UserId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }
    }

    [Route("/Sync/Options", "GET", Summary = "Gets a list of available sync targets.")]
    public class GetSyncDialogOptions : IReturn<SyncDialogOptions>
    {
        [ApiMember(Name = "UserId", Description = "UserId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string UserId { get; set; }

        [ApiMember(Name = "ItemIds", Description = "ItemIds", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ItemIds { get; set; }
    }

    [Route("/Sync/JobItems/{Id}/Transferred", "POST", Summary = "Reports that a sync job item has successfully been transferred.")]
    public class ReportSyncJobItemTransferred : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    [Route("/Sync/JobItems/{Id}/File", "GET", Summary = "Gets a sync job item file")]
    public class GetSyncJobItemFile
    {
        [ApiMember(Name = "Id", Description = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }
    }

    [Authenticated]
    public class SyncService : BaseApiService
    {
        private readonly ISyncManager _syncManager;
        private readonly IDtoService _dtoService;
        private readonly ILibraryManager _libraryManager;

        public SyncService(ISyncManager syncManager, IDtoService dtoService, ILibraryManager libraryManager)
        {
            _syncManager = syncManager;
            _dtoService = dtoService;
            _libraryManager = libraryManager;
        }

        public object Get(GetSyncTargets request)
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

        public void Post(ReportSyncJobItemTransferred request)
        {
            var task = _syncManager.ReportSyncJobItemTransferred(request.Id);

            Task.WaitAll(task);
        }

        public object Get(GetSyncJobItemFile request)
        {
            var jobItem = _syncManager.GetJobItem(request.Id);

            if (jobItem.Status != SyncJobItemStatus.Transferring)
            {
                throw new ArgumentException("The job item is not yet ready for transfer.");
            }

            return ToStaticFileResult(jobItem.OutputPath);
        }

        public object Get(GetSyncDialogOptions request)
        {
            var result = new SyncDialogOptions();

            result.Targets = _syncManager.GetSyncTargets(request.UserId)
                .ToList();

            var dtos = request.ItemIds.Split(',')
                .Select(_libraryManager.GetItemById)
                .Where(i => i != null)
                .Select(i => _dtoService.GetBaseItemDto(i, new DtoOptions
                {
                    Fields = new List<ItemFields>
                    {
                        ItemFields.SyncInfo
                    }
                }))
                .ToList();

            result.Options = SyncHelper.GetSyncOptions(dtos);

            return ToOptimizedResult(result);
        }
    }
}
