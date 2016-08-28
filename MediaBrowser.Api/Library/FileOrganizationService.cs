using System.Collections.Generic;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Api.Library
{
    [Route("/Library/FileOrganization", "GET", Summary = "Gets file organization results")]
    public class GetFileOrganizationActivity : IReturn<QueryResult<FileOrganizationResult>>
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

    [Route("/Library/FileOrganizations", "DELETE", Summary = "Clears the activity log")]
    public class ClearOrganizationLog : IReturnVoid
    {
    }

    [Route("/Library/FileOrganizations/{Id}/File", "DELETE", Summary = "Deletes the original file of a organizer result")]
    public class DeleteOriginalFile : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Result Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Library/FileOrganizations/{Id}/Organize", "POST", Summary = "Performs an organization")]
    public class PerformOrganization : IReturn<QueryResult<FileOrganizationResult>>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Result Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }

    [Route("/Library/FileOrganizations/{Id}/Episode/Organize", "POST", Summary = "Performs organization of a tv episode")]
    public class OrganizeEpisode
    {
        [ApiMember(Name = "Id", Description = "Result Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "SeriesId", Description = "Series Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string SeriesId { get; set; }

        [ApiMember(Name = "SeasonNumber", IsRequired = true, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int SeasonNumber { get; set; }

        [ApiMember(Name = "EpisodeNumber", IsRequired = true, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int EpisodeNumber { get; set; }

        [ApiMember(Name = "EndingEpisodeNumber", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int? EndingEpisodeNumber { get; set; }

        [ApiMember(Name = "RememberCorrection", Description = "Whether or not to apply the same correction to future episodes of the same series.", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool RememberCorrection { get; set; }

        [ApiMember(Name = "NewSeriesProviderIds", Description = "A list of provider IDs identifying a new series.", IsRequired = false, DataType = "Dictionary<string, string>", ParameterType = "query", Verb = "POST")]
        public Dictionary<string, string> NewSeriesProviderIds { get; set; }

        [ApiMember(Name = "NewSeriesName", Description = "Name of a series to add.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string NewSeriesName { get; set; }

        [ApiMember(Name = "NewSeriesYear", Description = "Year of a series to add.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string NewSeriesYear { get; set; }

        [ApiMember(Name = "TargetFolder", Description = "Target Folder", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string TargetFolder { get; set; }
    }

    [Route("/Library/FileOrganizations/SmartMatches", "GET", Summary = "Gets smart match entries")]
    public class GetSmartMatchInfos : IReturn<QueryResult<SmartMatchInfo>>
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

    [Route("/Library/FileOrganizations/SmartMatches/Delete", "POST", Summary = "Deletes a smart match entry")]
    public class DeleteSmartMatchEntry
    {
        [ApiMember(Name = "Entries", Description = "SmartMatch Entry", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public List<NameValuePair> Entries { get; set; }
    }

    [Authenticated(Roles = "Admin")]
    public class FileOrganizationService : BaseApiService
    {
        private readonly IFileOrganizationService _iFileOrganizationService;

        private readonly IJsonSerializer _jsonSerializer;

        public FileOrganizationService(IFileOrganizationService iFileOrganizationService, IJsonSerializer jsonSerializer)
        {
            _iFileOrganizationService = iFileOrganizationService;
            _jsonSerializer = jsonSerializer;
        }

        public object Get(GetFileOrganizationActivity request)
        {
            var result = _iFileOrganizationService.GetResults(new FileOrganizationResultQuery
            {
                Limit = request.Limit,
                StartIndex = request.StartIndex
            });

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public void Delete(DeleteOriginalFile request)
        {
            var task = _iFileOrganizationService.DeleteOriginalFile(request.Id);

            Task.WaitAll(task);
        }

        public void Delete(ClearOrganizationLog request)
        {
            var task = _iFileOrganizationService.ClearLog();

            Task.WaitAll(task);
        }

        public void Post(PerformOrganization request)
        {
            // Don't await this
            var task = _iFileOrganizationService.PerformOrganization(request.Id);

            // Async processing (close dialog early instead of waiting until the file has been copied)
            // Wait 2s for exceptions that may occur to have them forwarded to the client for immediate error display
            task.Wait(2000);
        }

        public void Post(OrganizeEpisode request)
        {
            var dicNewProviderIds = new Dictionary<string, string>();

            if (request.NewSeriesProviderIds != null)
            {
                dicNewProviderIds = request.NewSeriesProviderIds;
            }

            // Don't await this
            var task = _iFileOrganizationService.PerformEpisodeOrganization(new EpisodeFileOrganizationRequest
            {
                EndingEpisodeNumber = request.EndingEpisodeNumber,
                EpisodeNumber = request.EpisodeNumber,
                RememberCorrection = request.RememberCorrection,
                ResultId = request.Id,
                SeasonNumber = request.SeasonNumber,
                SeriesId = request.SeriesId,
                NewSeriesName = request.NewSeriesName,
                NewSeriesYear = request.NewSeriesYear,
                NewSeriesProviderIds = dicNewProviderIds,
                TargetFolder = request.TargetFolder
            });

            // Async processing (close dialog early instead of waiting until the file has been copied)
            // Wait 2s for exceptions that may occur to have them forwarded to the client for immediate error display
            task.Wait(2000);
        }

        public object Get(GetSmartMatchInfos request)
        {
            var result = _iFileOrganizationService.GetSmartMatchInfos(new FileOrganizationResultQuery
            {
                Limit = request.Limit,
                StartIndex = request.StartIndex
            });

            return ToOptimizedSerializedResultUsingCache(result);
        }

        public void Post(DeleteSmartMatchEntry request)
        {
            request.Entries.ForEach(entry =>
            {
                _iFileOrganizationService.DeleteSmartMatchEntry(entry.Name, entry.Value);
            });
        }
    }
}
