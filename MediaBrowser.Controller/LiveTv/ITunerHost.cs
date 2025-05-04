#nullable disable

#pragma warning disable CS1591

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Controller.LiveTv
{
    public interface ITunerHost
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>The type.</value>
        string Type { get; }

        bool IsSupported { get; }

        /// <summary>
        /// Gets the channels.
        /// </summary>
        /// <param name="enableCache">Option to enable using cache.</param>
        /// <param name="cancellationToken">The CancellationToken for this operation.</param>
        /// <returns>Task&lt;IEnumerable&lt;ChannelInfo&gt;&gt;.</returns>
        Task<List<ChannelInfo>> GetChannels(bool enableCache, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel stream.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="streamId">The stream identifier.</param>
        /// <param name="currentLiveStreams">The current live streams.</param>
        /// <param name="cancellationToken">The cancellation token to cancel operation.</param>
        /// <returns>Live stream wrapped in a task.</returns>
        Task<ILiveStream> GetChannelStream(string channelId, string streamId, IList<ILiveStream> currentLiveStreams, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel stream media sources.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;List&lt;MediaSourceInfo&gt;&gt;.</returns>
        Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken);

        Task<List<TunerHostInfo>> DiscoverDevices(int discoveryDurationMs, CancellationToken cancellationToken);
    }

    public interface IConfigurableTunerHost
    {
        /// <summary>
        /// Validates the specified information.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <returns>Task.</returns>
        Task Validate(TunerHostInfo info);
    }
}
