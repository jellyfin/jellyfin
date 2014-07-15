using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
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
        Task<List<SyncJob>> CreateJob(SyncJobRequest request);

        /// <summary>
        /// Creates the schedule.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task.</returns>
        Task<SyncSchedule> CreateSchedule(SyncScheduleRequest request);

        /// <summary>
        /// Gets the jobs.
        /// </summary>
        /// <returns>QueryResult&lt;SyncJob&gt;.</returns>
        QueryResult<SyncJob> GetJobs(SyncJobQuery query);

        /// <summary>
        /// Gets the schedules.
        /// </summary>
        /// <returns>QueryResult&lt;SyncSchedule&gt;.</returns>
        QueryResult<SyncSchedule> GetSchedules(SyncScheduleQuery query);

        /// <summary>
        /// Cancels the job.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task CancelJob(string id);

        /// <summary>
        /// Cancels the schedule.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task CancelSchedule(string id);
    }
}
