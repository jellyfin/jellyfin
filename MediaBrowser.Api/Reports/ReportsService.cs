using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using System.Threading.Tasks;

namespace MediaBrowser.Api.Reports
{
    public class ReportsService : BaseApiService
    {
        private readonly ILibraryManager _libraryManager;

        public ReportsService(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        public async Task<object> Get(GetItemReport request)
        {
            var queryResult = await GetQueryResult(request).ConfigureAwait(false);

            var reportResult = GetReportResult(queryResult);

            return ToOptimizedResult(reportResult);
        }

        private ReportResult GetReportResult(QueryResult<BaseItem> queryResult)
        {
            var reportResult = new ReportResult();

            // Fill rows and columns

            return reportResult;
        }

        private Task<QueryResult<BaseItem>> GetQueryResult(BaseReportRequest request)
        {
            // Placeholder in case needed later
            User user = null;

            var parentItem = string.IsNullOrEmpty(request.ParentId) ?
                (user == null ? _libraryManager.RootFolder : user.RootFolder) :
                _libraryManager.GetItemById(request.ParentId);

            return ((Folder)parentItem).GetItems(GetItemsQuery(request, user));
        }

        private InternalItemsQuery GetItemsQuery(BaseReportRequest request, User user)
        {
            var query = new InternalItemsQuery
            {
                User = user,
                CollapseBoxSetItems = false
            };

            // Set query values based on request

            // Example
            //query.IncludeItemTypes = new[] {"Movie"};


            return query;
        }
    }
}
