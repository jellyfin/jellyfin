using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using MediaBrowser.Model.Users;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Sync
{
    public interface ISyncManager
    {
        /// <summary>
        /// Creates the job.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        Task<SyncJobCreationResult> CreateJob(SyncJobRequest request);

        /// <summary>
        /// Gets the jobs.
        /// </summary>
        /// <returns>QueryResult&lt;SyncJob&gt;.</returns>
        Task<QueryResult<SyncJob>> GetJobs(SyncJobQuery query);

        /// <summary>
        /// Gets the job items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>QueryResult&lt;SyncJobItem&gt;.</returns>
        QueryResult<SyncJobItem> GetJobItems(SyncJobItemQuery query);
        
        /// <summary>
        /// Gets the job.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>SyncJob.</returns>
        SyncJob GetJob(string id);

        /// <summary>
        /// Updates the job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <returns>Task.</returns>
        Task UpdateJob(SyncJob job);

        /// <summary>
        /// Cancels the job.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task CancelJob(string id);

        /// <summary>
        /// Adds the parts.
        /// </summary>
        void AddParts(IEnumerable<ISyncProvider> providers);

        /// <summary>
        /// Gets the synchronize targets.
        /// </summary>
        IEnumerable<SyncTarget> GetSyncTargets(string userId);

        /// <summary>
        /// Supportses the synchronize.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool SupportsSync(BaseItem item);

        /// <summary>
        /// Gets the device profile.
        /// </summary>
        /// <param name="targetId">The target identifier.</param>
        /// <returns>DeviceProfile.</returns>
        DeviceProfile GetDeviceProfile(string targetId);

        /// <summary>
        /// Reports the synchronize job item transferred.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task ReportSyncJobItemTransferred(string id);

        /// <summary>
        /// Gets the job item.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>SyncJobItem.</returns>
        SyncJobItem GetJobItem(string id);

        /// <summary>
        /// Reports the offline action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>Task.</returns>
        Task ReportOfflineAction(UserAction action);

        /// <summary>
        /// Gets the ready synchronize items.
        /// </summary>
        /// <param name="targetId">The target identifier.</param>
        /// <returns>List&lt;SyncedItem&gt;.</returns>
        List<SyncedItem> GetReadySyncItems(string targetId);

        /// <summary>
        /// Synchronizes the data.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task&lt;SyncDataResponse&gt;.</returns>
        Task<SyncDataResponse> SyncData(SyncDataRequest request);
    }
}
