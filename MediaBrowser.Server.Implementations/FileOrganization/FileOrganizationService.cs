using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.FileOrganization;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.FileOrganization
{
    public class FileOrganizationService : IFileOrganizationService
    {
        private readonly ITaskManager _taskManager;
        private readonly IFileOrganizationRepository _repo;
        private readonly ILogger _logger;
        private readonly IDirectoryWatchers _directoryWatchers;
        private readonly ILibraryManager _libraryManager;

        public FileOrganizationService(ITaskManager taskManager, IFileOrganizationRepository repo, ILogger logger, IDirectoryWatchers directoryWatchers, ILibraryManager libraryManager)
        {
            _taskManager = taskManager;
            _repo = repo;
            _logger = logger;
            _directoryWatchers = directoryWatchers;
            _libraryManager = libraryManager;
        }

        public void BeginProcessNewFiles()
        {
            _taskManager.CancelIfRunningAndQueue<OrganizerScheduledTask>();
        }

        public Task SaveResult(FileOrganizationResult result, CancellationToken cancellationToken)
        {
            if (result == null || string.IsNullOrEmpty(result.OriginalPath))
            {
                throw new ArgumentNullException("result");
            }

            result.Id = (result.OriginalPath + (result.TargetPath ?? string.Empty)).GetMD5().ToString("N");

            return _repo.SaveResult(result, cancellationToken);
        }

        public QueryResult<FileOrganizationResult> GetResults(FileOrganizationResultQuery query)
        {
            return _repo.GetResults(query);
        }

        public Task DeleteOriginalFile(string resultId)
        {
            var result = _repo.GetResult(resultId);

            _logger.Info("Requested to delete {0}", result.OriginalPath);
            try
            {
                File.Delete(result.OriginalPath);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error deleting {0}", ex, result.OriginalPath);
            }

            return _repo.Delete(resultId);
        }

        public async Task PerformOrganization(string resultId)
        {
            var result = _repo.GetResult(resultId);

            if (string.IsNullOrEmpty(result.TargetPath))
            {
                throw new ArgumentException("No target path available.");
            }

            _logger.Info("Moving {0} to {1}", result.OriginalPath, result.TargetPath);

            _directoryWatchers.TemporarilyIgnore(result.TargetPath);

            var copy = File.Exists(result.TargetPath);

            try
            {
                if (copy)
                {
                    File.Copy(result.OriginalPath, result.TargetPath, true);
                }
                else
                {
                    File.Move(result.OriginalPath, result.TargetPath);
                }
            }
            finally
            {
                _directoryWatchers.RemoveTempIgnore(result.TargetPath);
            }

            if (copy)
            {
                try
                {
                    File.Delete(result.OriginalPath);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error deleting {0}", ex, result.OriginalPath);
                }
            }

            result.Status = FileSortingStatus.Success;
            result.StatusMessage = string.Empty;

            await SaveResult(result, CancellationToken.None).ConfigureAwait(false);

            await _libraryManager.ValidateMediaLibrary(new Progress<double>(), CancellationToken.None)
                    .ConfigureAwait(false);
        }
    }
}
