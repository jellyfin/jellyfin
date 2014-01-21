using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Querying;
using ServiceStack;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Library
{
    [Route("/Library/FileOrganization", "GET")]
    [Api(Description = "Gets file organization results")]
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

    [Route("/Library/FileOrganizations/{Id}/File", "DELETE")]
    [Api(Description = "Deletes the original file of a organizer result")]
    public class DeleteOriginalFile : IReturn<QueryResult<FileOrganizationResult>>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Result Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Library/FileOrganizations/{Id}/Organize", "POST")]
    [Api(Description = "Performs an organization")]
    public class PerformOrganization : IReturn<QueryResult<FileOrganizationResult>>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Result Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "POST")]
        public string Id { get; set; }
    }
    
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

            return ToOptimizedResult(result);
        }

        public void Delete(DeleteOriginalFile request)
        {
            var task = _iFileOrganizationService.DeleteOriginalFile(request.Id);

            Task.WaitAll(task);
        }

        public void Post(PerformOrganization request)
        {
            var task = _iFileOrganizationService.PerformOrganization(request.Id);

            Task.WaitAll(task);
        }
    }
}
