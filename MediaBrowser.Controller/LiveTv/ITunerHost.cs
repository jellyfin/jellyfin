using System;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        /// <summary>
        /// Gets the channels.
        /// </summary>
        /// <returns>Task&lt;IEnumerable&lt;ChannelInfo&gt;&gt;.</returns>
        Task<IEnumerable<ChannelInfo>> GetChannels(bool enableCache, CancellationToken cancellationToken);
        /// <summary>
        /// Gets the tuner infos.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;List&lt;LiveTvTunerInfo&gt;&gt;.</returns>
        Task<List<LiveTvTunerInfo>> GetTunerInfos(CancellationToken cancellationToken);
        /// <summary>
        /// Gets the channel stream.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="streamId">The stream identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;MediaSourceInfo&gt;.</returns>
        Task<LiveStream> GetChannelStream(string channelId, string streamId, CancellationToken cancellationToken);
        /// <summary>
        /// Gets the channel stream media sources.
        /// </summary>
        /// <param name="channelId">The channel identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;List&lt;MediaSourceInfo&gt;&gt;.</returns>
        Task<List<MediaSourceInfo>> GetChannelStreamMediaSources(string channelId, CancellationToken cancellationToken);
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
