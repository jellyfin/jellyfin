using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Channels
{
    public interface IChannelManager
    {
        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="channels">The channels.</param>
        /// <param name="factories">The factories.</param>
        void AddParts(IEnumerable<IChannel> channels, IEnumerable<IChannelFactory> factories);

        /// <summary>
        /// Gets the channel download path.
        /// </summary>
        /// <value>The channel download path.</value>
        string ChannelDownloadPath { get; }

        /// <summary>
        /// Gets the channel features.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>ChannelFeatures.</returns>
        ChannelFeatures GetChannelFeatures(string id);

        /// <summary>
        /// Gets all channel features.
        /// </summary>
        /// <returns>IEnumerable{ChannelFeatures}.</returns>
        IEnumerable<ChannelFeatures> GetAllChannelFeatures();

        /// <summary>
        /// Gets the channel.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>Channel.</returns>
        Channel GetChannel(string id);

        /// <summary>
        /// Gets the channels.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{BaseItemDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetChannels(ChannelQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all media.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{BaseItemDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetAllMedia(AllChannelMediaQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{BaseItemDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetChannelItems(ChannelItemQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel item media sources.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{ChannelMediaInfo}}.</returns>
        Task<IEnumerable<MediaSourceInfo>> GetChannelItemMediaSources(string id, CancellationToken cancellationToken);
    }
}
