using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.LiveTv
{
    /// <summary>
    /// Manages all live tv services installed on the server
    /// </summary>
    public interface ILiveTvManager
    {
        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        IReadOnlyList<ILiveTvService> Services { get; }

        /// <summary>
        /// Schedules the recording.
        /// </summary>
        /// <param name="programId">The program identifier.</param>
        /// <returns>Task.</returns>
        Task ScheduleRecording(string programId);

        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="services">The services.</param>
        void AddParts(IEnumerable<ILiveTvService> services);

        /// <summary>
        /// Gets the channels.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable{Channel}.</returns>
        QueryResult<ChannelInfoDto> GetChannels(ChannelQuery query);

        /// <summary>
        /// Gets the recordings.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>QueryResult{RecordingInfoDto}.</returns>
        Task<QueryResult<RecordingInfoDto>> GetRecordings(RecordingQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the timers.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{TimerInfoDto}}.</returns>
        Task<QueryResult<TimerInfoDto>> GetTimers(TimerQuery query, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Channel.</returns>
        Channel GetChannel(string id);

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>Channel.</returns>
        ChannelInfoDto GetChannelInfoDto(string id, string userId);

        /// <summary>
        /// Gets the programs.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IEnumerable{ProgramInfo}.</returns>
        Task<QueryResult<ProgramInfoDto>> GetPrograms(ProgramQuery query, CancellationToken cancellationToken);
    }
}
