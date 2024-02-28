#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.LiveTv
{
    /// <summary>
    /// Manages all live tv services installed on the server.
    /// </summary>
    public interface ILiveTvManager
    {
        event EventHandler<GenericEventArgs<TimerEventInfo>> SeriesTimerCancelled;

        event EventHandler<GenericEventArgs<TimerEventInfo>> TimerCancelled;

        event EventHandler<GenericEventArgs<TimerEventInfo>> TimerCreated;

        event EventHandler<GenericEventArgs<TimerEventInfo>> SeriesTimerCreated;

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
        /// <param name="options">The options.</param>
        /// <returns>A recording.</returns>
        Task<QueryResult<BaseItemDto>> GetRecordingsAsync(RecordingQuery query, DtoOptions options);

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
        /// Gets the program.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task{ProgramInfoDto}.</returns>
        Task<BaseItemDto> GetProgram(string id, CancellationToken cancellationToken, User user = null);

        /// <summary>
        /// Gets the programs.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IEnumerable{ProgramInfo}.</returns>
        Task<QueryResult<BaseItemDto>> GetPrograms(InternalItemsQuery query, DtoOptions options, CancellationToken cancellationToken);

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
        /// Gets the recommended programs.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Recommended programs.</returns>
        Task<QueryResult<BaseItemDto>> GetRecommendedProgramsAsync(InternalItemsQuery query, DtoOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recommended programs internal.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Recommended programs.</returns>
        QueryResult<BaseItem> GetRecommendedProgramsInternal(InternalItemsQuery query, DtoOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live tv information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{LiveTvInfo}.</returns>
        LiveTvInfo GetLiveTvInfo(CancellationToken cancellationToken);

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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Live TV folder.</returns>
        Folder GetInternalLiveTvFolder(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the enabled users.
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        IEnumerable<User> GetEnabledUsers();

        /// <summary>
        /// Gets the internal channels.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="dtoOptions">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Internal channels.</returns>
        QueryResult<BaseItem> GetInternalChannels(LiveTvChannelQuery query, DtoOptions dtoOptions, CancellationToken cancellationToken);

        /// <summary>
        /// Adds the information to program dto.
        /// </summary>
        /// <param name="programs">The programs.</param>
        /// <param name="fields">The fields.</param>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        Task AddInfoToProgramDto(IReadOnlyCollection<(BaseItem Item, BaseItemDto ItemDto)> programs, IReadOnlyList<ItemFields> fields, User user = null);

        /// <summary>
        /// Adds the channel information.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="options">The options.</param>
        /// <param name="user">The user.</param>
        void AddChannelInfo(IReadOnlyCollection<(BaseItemDto ItemDto, LiveTvChannel Channel)> items, DtoOptions options, User user);

        void AddInfoToRecordingDto(BaseItem item, BaseItemDto dto, ActiveRecordingInfo activeRecordingInfo, User user = null);

        Task<BaseItem[]> GetRecordingFoldersAsync(User user);
    }
}
