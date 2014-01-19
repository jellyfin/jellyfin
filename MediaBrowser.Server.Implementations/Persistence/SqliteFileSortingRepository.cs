using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.FileSorting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class SqliteFileSortingRepository : IFileSortingRepository
    {
        public Task SaveResult(FileSortingResult result, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public IEnumerable<FileSortingResult> GetResults(FileSortingResultQuery query)
        {
            return new List<FileSortingResult>();
        }
    }
}
