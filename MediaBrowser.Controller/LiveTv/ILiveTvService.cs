using MediaBrowser.Model.LiveTv;
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
        /// Gets the recordings asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RecordingInfo}}.</returns>
        Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the epg asynchronous.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{EpgFullInfo}}.</returns>
        Task<IEnumerable<EpgFullInfo>> GetEpgAsync(string channelId, CancellationToken cancellationToken);
    }
}
