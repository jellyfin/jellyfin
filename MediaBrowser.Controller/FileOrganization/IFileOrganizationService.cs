using MediaBrowser.Model.FileOrganization;
using System.Collections.Generic;
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
        /// Gets the results.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable{FileOrganizationResult}.</returns>
        IEnumerable<FileOrganizationResult> GetResults(FileOrganizationResultQuery query);
    }
}
