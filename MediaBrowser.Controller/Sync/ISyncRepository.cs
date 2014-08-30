using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Sync
{
    public interface ISyncRepository
    {
        /// <summary>
        /// Gets the job.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>SyncJob.</returns>
        SyncJob GetJob(string id);

        /// <summary>
        /// Creates the specified job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <returns>Task.</returns>
        Task Create(SyncJob job);

        /// <summary>
        /// Updates the specified job.
        /// </summary>
        /// <param name="job">The job.</param>
        /// <returns>Task.</returns>
        Task Update(SyncJob job);

        /// <summary>
        /// Gets the jobs.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>QueryResult&lt;SyncJob&gt;.</returns>
        QueryResult<SyncJob> GetJobs(SyncJobQuery query);

        /// <summary>
        /// Gets the job item.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>SyncJobItem.</returns>
        SyncJobItem GetJobItem(string id);

        /// <summary>
        /// Creates the specified job item.
        /// </summary>
        /// <param name="jobItem">The job item.</param>
        /// <returns>Task.</returns>
        Task Create(SyncJobItem jobItem);

        /// <summary>
        /// Updates the specified job item.
        /// </summary>
        /// <param name="jobItem">The job item.</param>
        /// <returns>Task.</returns>
        Task Update(SyncJobItem jobItem);
    }
}
