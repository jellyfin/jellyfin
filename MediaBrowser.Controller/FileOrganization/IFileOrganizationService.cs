using MediaBrowser.Model.Events;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Querying;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.FileOrganization
{
    public interface IFileOrganizationService
    {
        event EventHandler<GenericEventArgs<FileOrganizationResult>> ItemAdded;
        event EventHandler<GenericEventArgs<FileOrganizationResult>> ItemUpdated;
        event EventHandler<GenericEventArgs<FileOrganizationResult>> ItemRemoved;
        event EventHandler LogReset;

        /// <summary>
        /// Processes the new files.
        /// </summary>
        void BeginProcessNewFiles();

        /// <summary>
        /// Deletes the original file.
        /// </summary>
        /// <param name="resultId">The result identifier.</param>
        /// <returns>Task.</returns>
        Task DeleteOriginalFile(string resultId);

        /// <summary>
        /// Clears the log.
        /// </summary>
        /// <returns>Task.</returns>
        Task ClearLog();
        
        /// <summary>
        /// Performs the organization.
        /// </summary>
        /// <param name="resultId">The result identifier.</param>
        /// <returns>Task.</returns>
        Task PerformOrganization(string resultId);

        /// <summary>
        /// Performs the episode organization.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        Task PerformEpisodeOrganization(EpisodeFileOrganizationRequest request);
        
        /// <summary>
        /// Gets the results.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable{FileOrganizationResult}.</returns>
        QueryResult<FileOrganizationResult> GetResults(FileOrganizationResultQuery query);

        /// <summary>
        /// Gets the result.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>FileOrganizationResult.</returns>
        FileOrganizationResult GetResult(string id);

        /// <summary>
        /// Gets the result by source path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>FileOrganizationResult.</returns>
        FileOrganizationResult GetResultBySourcePath(string path);
        
        /// <summary>
        /// Saves the result.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveResult(FileOrganizationResult result, CancellationToken cancellationToken);

        /// <summary>
        /// Returns a list of smart match entries
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable{SmartMatchInfo}.</returns>
        QueryResult<SmartMatchInfo> GetSmartMatchInfos(FileOrganizationResultQuery query);

        /// <summary>
        /// Deletes a smart match entry.
        /// </summary>
        /// <param name="ItemName">Item name.</param>
        /// <param name="matchString">The match string to delete.</param>
        void DeleteSmartMatchEntry(string ItemName, string matchString);

        /// <summary>
        /// Attempts to add a an item to the list of currently processed items.
        /// </summary>
        /// <param name="result">The result item.</param>
        /// <param name="fullClientRefresh">Passing true will notify the client to reload all items, otherwise only a single item will be refreshed.</param>
        /// <returns>True if the item was added, False if the item is already contained in the list.</returns>
        bool AddToInProgressList(FileOrganizationResult result, bool fullClientRefresh);

        /// <summary>
        /// Removes an item from the list of currently processed items.
        /// </summary>
        /// <param name="result">The result item.</param>
        /// <returns>True if the item was removed, False if the item was not contained in the list.</returns>
        bool RemoveFromInprogressList(FileOrganizationResult result);
    }
}
