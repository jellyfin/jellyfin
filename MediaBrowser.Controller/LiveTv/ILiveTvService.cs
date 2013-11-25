using MediaBrowser.Common.Net;
using MediaBrowser.Model.LiveTv;
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
        /// Cancels the recording asynchronous.
        /// </summary>
        /// <param name="recordingId">The recording identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task CancelRecordingAsync(string recordingId, CancellationToken cancellationToken);

        /// <summary>
        /// Schedules the recording asynchronous.
        /// </summary>
        /// <param name="name">The name for the recording</param>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ScheduleRecordingAsync(string name,string channelId, DateTime startTime, TimeSpan duration, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets the channel image asynchronous.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<HttpResponseInfo> GetChannelImageAsync(string channelId, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the recordings asynchronous.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RecordingInfo}}.</returns>
        Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(RecordingQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel guide.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ProgramInfo}}.</returns>
        Task<IEnumerable<ProgramInfo>> GetChannelGuideAsync(string channelId, CancellationToken cancellationToken);
    }
}
