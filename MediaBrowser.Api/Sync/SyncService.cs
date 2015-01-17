using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using MediaBrowser.Model.Users;
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

    [Route("/Sync/Jobs/{Id}", "POST", Summary = "Updates a sync job.")]
    public class UpdateSyncJob : SyncJob, IReturnVoid
    {
    }

    [Route("/Sync/JobItems", "GET", Summary = "Gets sync job items.")]
    public class GetSyncJobItems : SyncJobItemQuery, IReturn<QueryResult<SyncJobItem>>
    {
    }

    [Route("/Sync/Jobs", "GET", Summary = "Gets sync jobs.")]
    public class GetSyncJobs : SyncJobQuery, IReturn<QueryResult<SyncJob>>
    {
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

        [ApiMember(Name = "ItemIds", Description = "ItemIds", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ItemIds { get; set; }

        [ApiMember(Name = "ParentId", Description = "ParentId", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string ParentId { get; set; }

        [ApiMember(Name = "Category", Description = "Category", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public SyncCategory? Category { get; set; }
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

    [Route("/Sync/JobItems/{Id}/AdditionalFiles", "GET", Summary = "Gets a sync job item file")]
    public class GetSyncJobItemAdditionalFile
    {
        [ApiMember(Name = "Id", Description = "Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

        [ApiMember(Name = "Name", Description = "Name", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Name { get; set; }
    }

    [Route("/Sync/OfflineActions", "POST", Summary = "Reports an action that occurred while offline.")]
    public class ReportOfflineActions : List<UserAction>, IReturnVoid
    {
    }

    [Route("/Sync/Items/Ready", "GET", Summary = "Gets ready to download sync items.")]
    public class GetReadySyncItems : IReturn<List<SyncedItem>>
    {
        [ApiMember(Name = "TargetId", Description = "TargetId", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string TargetId { get; set; }
    }

    [Route("/Sync/Data", "POST", Summary = "Syncs data between device and server")]
    public class SyncData : SyncDataRequest, IReturn<SyncDataResponse>
    {
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

        public async Task<object> Get(GetSyncJobs request)
        {
            var result = await _syncManager.GetJobs(request).ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public object Get(GetSyncJobItems request)
        {
            var result = _syncManager.GetJobItems(request);

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

            if (request.Category.HasValue)
            {
                result.Options = SyncHelper.GetSyncOptions(request.Category.Value);
            }
            else
            {
                var dtoOptions = new DtoOptions
                {
                    Fields = new List<ItemFields>
                    {
                        ItemFields.SyncInfo
                    }
                };

                var dtos = request.ItemIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(_libraryManager.GetItemById)
                    .Where(i => i != null)
                    .Select(i => _dtoService.GetBaseItemDto(i, dtoOptions))
                    .ToList();

                result.Options = SyncHelper.GetSyncOptions(dtos);
            }

            return ToOptimizedResult(result);
        }

        public void Post(ReportOfflineActions request)
        {
            var task = PostAsync(request);

            Task.WaitAll(task);
        }

        public async Task PostAsync(ReportOfflineActions request)
        {
            foreach (var action in request)
            {
                await _syncManager.ReportOfflineAction(action).ConfigureAwait(false);
            }
        }

        public object Get(GetReadySyncItems request)
        {
            return ToOptimizedResult(_syncManager.GetReadySyncItems(request.TargetId));
        }

        public async Task<object> Post(SyncData request)
        {
            var response = await _syncManager.SyncData(request).ConfigureAwait(false);

            return ToOptimizedResult(response);
        }

        public void Post(UpdateSyncJob request)
        {
            var task = _syncManager.UpdateJob(request);

            Task.WaitAll(task);
        }

        public object Get(GetSyncJobItemAdditionalFile request)
        {
            var jobItem = _syncManager.GetJobItem(request.Id);

            if (jobItem.Status != SyncJobItemStatus.Transferring)
            {
                throw new ArgumentException("The job item is not yet ready for transfer.");
            }

            var file = jobItem.AdditionalFiles.FirstOrDefault(i => string.Equals(i.Name, request.Name, StringComparison.OrdinalIgnoreCase));

            if (file == null)
            {
                throw new ArgumentException("Sync job additional file not found.");
            }

            return ToStaticFileResult(file.Path);
        }
    }
}
