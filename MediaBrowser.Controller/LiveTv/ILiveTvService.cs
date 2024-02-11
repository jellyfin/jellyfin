#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;

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
        /// Gets the home page URL.
        /// </summary>
        /// <value>The home page URL.</value>
        string HomePageUrl { get; }

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
        /// <param name="updatedTimer">The updated timer information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateTimerAsync(TimerInfo updatedTimer, CancellationToken cancellationToken);

        /// <summary>
        /// Updates the series timer asynchronous.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateSeriesTimerAsync(SeriesTimerInfo info, CancellationToken cancellationToken);

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
        /// Closes the live stream.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CloseLiveStream(string id, CancellationToken cancellationToken);

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

    public interface ISupportsDirectStreamProvider
    {
        Task<ILiveStream> GetChannelStreamWithDirectStreamProvider(string channelId, string streamId, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken);
    }
}
