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
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

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
        /// Gets the channel image asynchronous.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<ImageResponseInfo> GetChannelImageAsync(string channelId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recording image asynchronous.
        /// </summary>
        /// <param name="recordingId">The recording identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ImageResponseInfo}.</returns>
        Task<ImageResponseInfo> GetRecordingImageAsync(string recordingId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the program image asynchronous.
        /// </summary>
        /// <param name="programId">The program identifier.</param>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{ImageResponseInfo}.</returns>
        Task<ImageResponseInfo> GetProgramImageAsync(string programId, string channelId, CancellationToken cancellationToken);
        
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
        /// Gets the timer defaults asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{TimerInfo}.</returns>
        Task<SeriesTimerInfo> GetNewTimerDefaultsAsync(CancellationToken cancellationToken);
        
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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ProgramInfo}}.</returns>
        Task<IEnumerable<ProgramInfo>> GetProgramsAsync(string channelId, CancellationToken cancellationToken);
    }
}
