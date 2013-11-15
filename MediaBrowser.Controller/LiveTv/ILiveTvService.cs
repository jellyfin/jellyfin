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
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RecordingInfo}}.</returns>
        Task<IEnumerable<RecordingInfo>> GetRecordingsAsync(RecordingQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel guides.
        /// </summary>
        /// <param name="channelIdList">The channel identifier list.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelGuide}}.</returns>
        Task<IEnumerable<ChannelGuide>> GetChannelGuidesAsync(IEnumerable<string> channelIdList, CancellationToken cancellationToken);
    }
}
