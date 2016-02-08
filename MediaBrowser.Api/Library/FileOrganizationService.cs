using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System.Threading.Tasks;

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

    [Route("/Library/FileOrganizations/{Id}/Episode/Organize", "POST", Summary = "Performs an organization")]
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
    }

    [Route("/Library/FileOrganizationSmartMatch", "GET", Summary = "Gets smart match entries")]
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

    [Route("/Library/FileOrganizationSmartMatch/{Id}/Delete", "POST", Summary = "Deletes a smart match entry")]
    public class DeleteSmartMatchEntry
    {
        [ApiMember(Name = "Id", Description = "Item ID", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "MatchString", Description = "SmartMatch String", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string MatchString { get; set; }
    }

    [Authenticated(Roles = "Admin")]
    public class FileOrganizationService : BaseApiService
    {
        private readonly IFileOrganizationService _iFileOrganizationService;

        public FileOrganizationService(IFileOrganizationService iFileOrganizationService)
        {
            _iFileOrganizationService = iFileOrganizationService;
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
            var task = _iFileOrganizationService.PerformOrganization(request.Id);

            Task.WaitAll(task);
        }

        public void Post(OrganizeEpisode request)
        {
            var task = _iFileOrganizationService.PerformEpisodeOrganization(new EpisodeFileOrganizationRequest
            {
                EndingEpisodeNumber = request.EndingEpisodeNumber,
                EpisodeNumber = request.EpisodeNumber,
                RememberCorrection = request.RememberCorrection,
                ResultId = request.Id,
                SeasonNumber = request.SeasonNumber,
                SeriesId = request.SeriesId
            });

            Task.WaitAll(task);
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
            _iFileOrganizationService.DeleteSmartMatchEntry(request.Id, request.MatchString);
        }
    }
}
