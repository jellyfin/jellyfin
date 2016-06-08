using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.LiveTv
{
    /// <summary>
    /// Represents a single live tv back end (next pvr, media portal, etc).
    /// </summary>
    public interface ILiveTvService
    {
        /// <summary>
        /// Occurs when [data source changed].
        /// </summary>
        event EventHandler DataSourceChanged;

        /// <summary>
        /// Occurs when [recording status changed].
        /// </summary>
        event EventHandler<RecordingStatusChangedEventArgs> RecordingStatusChanged;

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the home page URL.
        /// </summary>
        /// <value>The home page URL.</value>
        string HomePageUrl { get; }

        /// <summary>
        /// Gets the status information asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{LiveTvServiceStatusInfo}.</returns>
        Task<LiveTvServiceStatusInfo> GetStatusInfoAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channels async.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelInfo}}.</returns>
        Task<IEnumerable<ChannelInfo>> GetChannelsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Cancels the timer asynchronous.
        /// </summary>
        /// <param name="timerId">The timer identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CancelTimerAsync(string timerId, CancellationToken cancellationToken);

        /// <summary>
        /// Cancels the series timer asynchronous.
        /// </summary>
        /// <param name="timerId">The timer identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CancelSeriesTimerAsync(string timerId, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the recording asynchronous.
        /// </summary>
        /// <param name="recordingId">The recording identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteRecordingAsync(string recordingId, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the timer asynchronous.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CreateTimerAsync(TimerInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the series timer asynchronous.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CreateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the timer asynchronous.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateTimerAsync(TimerInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the series timer asynchronous.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel image asynchronous. This only needs to be implemented if an image path or url cannot be supplied to ChannelInfo
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<ImageStream> GetChannelImageAsync(string channelId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recording image asynchronous. This only needs to be implemented if an image path or url cannot be supplied to RecordingInfo
        /// </summary>
        /// <param name="recordingId">The recording identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ImageResponseInfo}.</returns>
        Task<ImageStream> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the program image asynchronous. This only needs to be implemented if an image path or url cannot be supplied to ProgramInfo
        /// </summary>
        /// <param name="programId">The program identifier.</param>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ImageResponseInfo}.</returns>
        Task<ImageStream> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recordings asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RecordingInfo}}.</returns>
        Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recordings asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RecordingInfo}}.</returns>
        Task<IEnumerable<TimerInfo>> GetTimersAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the new timer defaults asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="program">The program.</param>
        /// <returns>Task{SeriesTimerInfo}.</returns>
        Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken, ProgramInfo program = null);
        
        /// <summary>
        /// Gets the series timers asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{SeriesTimerInfo}}.</returns>
        Task<IEnumerable<SeriesTimerInfo>> GetSeriesTimersAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the programs asynchronous.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="startDateUtc">The start date UTC.</param>
        /// <param name="endDateUtc">The end date UTC.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ProgramInfo}}.</returns>
        Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, DateTime startDateUtc, DateTime endDateUtc, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recording stream.
        /// </summary>
        /// <param name="recordingId">The recording identifier.</param>
        /// <param name="streamId">The stream identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<MediaSourceInfo> GetRecordingStream(string recordingId, string streamId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel stream.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="streamId">The stream identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<MediaSourceInfo> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel stream media sources.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;List&lt;MediaSourceInfo&gt;&gt;.</returns>
        Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recording stream media sources.
        /// </summary>
        /// <param name="recordingId">The recording identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;List&lt;MediaSourceInfo&gt;&gt;.</returns>
        Task<List<MediaSourceInfo>> GetRecordingStreamMediaSources(string recordingId, CancellationToken cancellationToken);
        
        /// <summary>
        /// Closes the live stream.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CloseLiveStream(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Records the live stream.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task RecordLiveStream(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Resets the tuner.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ResetTuner(string id, CancellationToken cancellationToken);
    }

    public interface ISupportsNewTimerIds
    {
        /// <summary>
        /// Creates the timer asynchronous.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task<string> CreateTimer(TimerInfo info, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the series timer asynchronous.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task<string> CreateSeriesTimer(SeriesTimerInfo info, CancellationToken cancellationToken);
    }
}
