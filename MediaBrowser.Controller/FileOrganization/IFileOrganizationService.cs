using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Querying;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.FileOrganization
{
    public interface IFileOrganizationService
    {
        /// <summary>
        /// Processes the new files.
        /// </summary>
        void BeginProcessNewFiles();

        /// <summary>
        /// Saves the result.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveResult(FileOrganizationResult result, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the original file.
        /// </summary>
        /// <param name="resultId">The result identifier.</param>
        /// <returns>Task.</returns>
        Task DeleteOriginalFile(string resultId);

        /// <summary>
        /// Performs the organization.
        /// </summary>
        /// <param name="resultId">The result identifier.</param>
        /// <returns>Task.</returns>
        Task PerformOrganization(string resultId);
        
        /// <summary>
        /// Gets the results.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable{FileOrganizationResult}.</returns>
        QueryResult<FileOrganizationResult> GetResults(FileOrganizationResultQuery query);
    }
}
