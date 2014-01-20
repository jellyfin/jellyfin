using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Querying;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.FileOrganization
{
    public class FileOrganizationService : IFileOrganizationService
    {
        private readonly ITaskManager _taskManager;
        private readonly IFileOrganizationRepository _repo;

        public FileOrganizationService(ITaskManager taskManager, IFileOrganizationRepository repo)
        {
            _taskManager = taskManager;
            _repo = repo;
        }

        public void BeginProcessNewFiles()
        {
             _taskManager.CancelIfRunningAndQueue<OrganizerScheduledTask>();
        }


        public Task SaveResult(FileOrganizationResult result, CancellationToken cancellationToken)
        {
            return _repo.SaveResult(result, cancellationToken);
        }

        public QueryResult<FileOrganizationResult> GetResults(FileOrganizationResultQuery query)
        {
            return _repo.GetResults(query);
        }
    }
}
