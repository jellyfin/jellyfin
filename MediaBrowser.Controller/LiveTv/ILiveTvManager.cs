using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
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
        /// Gets the active service.
        /// </summary>
        /// <value>The active service.</value>
        ILiveTvService ActiveService { get; }

        /// <summary>
        /// Gets the services.
        /// </summary>
        /// <value>The services.</value>
        IReadOnlyList<ILiveTvService> Services { get; }

        /// <summary>
        /// Gets the new timer defaults asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{TimerInfo}.</returns>
        Task<SeriesTimerInfoDto> GetNewTimerDefaults(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the new timer defaults.
        /// </summary>
        /// <param name="programId">The program identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{SeriesTimerInfoDto}.</returns>
        Task<SeriesTimerInfoDto> GetNewTimerDefaults(string programId, CancellationToken cancellationToken);
        
        /// <summary>
        /// Deletes the recording.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task DeleteRecording(string id);

        /// <summary>
        /// Cancels the timer.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task CancelTimer(string id);

        /// <summary>
        /// Cancels the series timer.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task CancelSeriesTimer(string id);
        
        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="services">The services.</param>
        void AddParts(IEnumerable<ILiveTvService> services);

        /// <summary>
        /// Gets the channels.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IEnumerable{Channel}.</returns>
        Task<QueryResult<ChannelInfoDto>> GetChannels(LiveTvChannelQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recording.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{RecordingInfoDto}.</returns>
        Task<RecordingInfoDto> GetRecording(string id, CancellationToken cancellationToken, User user = null);

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task{RecordingInfoDto}.</returns>
        Task<ChannelInfoDto> GetChannel(string id, CancellationToken cancellationToken, User user = null);
        
        /// <summary>
        /// Gets the timer.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{TimerInfoDto}.</returns>
        Task<TimerInfoDto> GetTimer(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the series timer.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{TimerInfoDto}.</returns>
        Task<SeriesTimerInfoDto> GetSeriesTimer(string id, CancellationToken cancellationToken);
        
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
        /// Gets the series timers.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{SeriesTimerInfoDto}}.</returns>
        Task<QueryResult<SeriesTimerInfoDto>> GetSeriesTimers(SeriesTimerQuery query, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Channel.</returns>
        LiveTvChannel GetInternalChannel(string id);
        
        /// <summary>
        /// Gets the recording.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>LiveTvRecording.</returns>
        Task<ILiveTvRecording> GetInternalRecording(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recording stream.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<LiveStreamInfo> GetRecordingStream(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel stream.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{StreamResponseInfo}.</returns>
        Task<LiveStreamInfo> GetChannelStream(string id, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets the program.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task{ProgramInfoDto}.</returns>
        Task<ProgramInfoDto> GetProgram(string id, CancellationToken cancellationToken, User user = null);
        
        /// <summary>
        /// Gets the programs.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IEnumerable{ProgramInfo}.</returns>
        Task<QueryResult<ProgramInfoDto>> GetPrograms(ProgramQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the timer.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateTimer(TimerInfoDto timer, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the timer.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the timer.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CreateTimer(TimerInfoDto timer, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the series timer.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CreateSeriesTimer(SeriesTimerInfoDto timer, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recording groups.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{RecordingGroupDto}}.</returns>
        Task<QueryResult<RecordingGroupDto>> GetRecordingGroups(RecordingGroupQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Closes the live stream.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CloseLiveStream(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the guide information.
        /// </summary>
        /// <returns>GuideInfo.</returns>
        GuideInfo GetGuideInfo();

        /// <summary>
        /// Gets the recommended programs.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{ProgramInfoDto}}.</returns>
        Task<QueryResult<ProgramInfoDto>> GetRecommendedPrograms(RecommendedProgramQuery query,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{LiveTvInfo}.</returns>
        Task<LiveTvInfo> GetLiveTvInfo(CancellationToken cancellationToken);

        /// <summary>
        /// Resets the tuner.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ResetTuner(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv folder.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>BaseItemDto.</returns>
        Task<Folder> GetInternalLiveTvFolder(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv folder.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>BaseItemDto.</returns>
        Task<BaseItemDto> GetLiveTvFolder(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the enabled users.
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        IEnumerable<User> GetEnabledUsers();

        /// <summary>
        /// Gets the internal channels.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;QueryResult&lt;LiveTvChannel&gt;&gt;.</returns>
        Task<QueryResult<LiveTvChannel>> GetInternalChannels(LiveTvChannelQuery query,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the internal recordings.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;QueryResult&lt;BaseItem&gt;&gt;.</returns>
        Task<QueryResult<BaseItem>> GetInternalRecordings(RecordingQuery query, CancellationToken cancellationToken);
    }
}
