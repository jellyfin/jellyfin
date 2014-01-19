using MediaBrowser.Model.FileSorting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Persistence
{
    public interface IFileSortingRepository
    {
        /// <summary>
        /// Saves the result.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveResult(FileSortingResult result, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the results.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable{FileSortingResult}.</returns>
        IEnumerable<FileSortingResult> GetResults(FileSortingResultQuery query);
    }
}
