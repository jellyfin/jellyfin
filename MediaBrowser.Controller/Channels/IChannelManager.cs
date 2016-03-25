using System;
using MediaBrowser.Controller.Entities;
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
        void AddParts(IEnumerable<IChannel> channels);

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
        /// Gets the channels internal.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;QueryResult&lt;Channel&gt;&gt;.</returns>
        Task<QueryResult<Channel>> GetChannelsInternal(ChannelQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channels.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{BaseItemDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetChannels(ChannelQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets all media internal.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;QueryResult&lt;BaseItem&gt;&gt;.</returns>
        Task<QueryResult<BaseItem>> GetAllMediaInternal(AllChannelMediaQuery query, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets all media.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{BaseItemDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetAllMedia(AllChannelMediaQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the latest media.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{BaseItemDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetLatestChannelItems(AllChannelMediaQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the latest channel items internal.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;QueryResult&lt;BaseItem&gt;&gt;.</returns>
        Task<QueryResult<BaseItem>> GetLatestChannelItemsInternal(AllChannelMediaQuery query, CancellationToken cancellationToken);
        
        /// <summary>
        /// Gets the channel items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{QueryResult{BaseItemDto}}.</returns>
        Task<QueryResult<BaseItemDto>> GetChannelItems(ChannelItemQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel items internal.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;QueryResult&lt;BaseItem&gt;&gt;.</returns>
        Task<QueryResult<BaseItem>> GetChannelItemsInternal(ChannelItemQuery query, IProgress<double> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel item media sources.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="includeCachedVersions">if set to <c>true</c> [include cached versions].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{MediaSourceInfo}}.</returns>
        Task<IEnumerable<MediaSourceInfo>> GetStaticMediaSources(BaseItem item, bool includeCachedVersions, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel folder.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>BaseItemDto.</returns>
        Task<Folder> GetInternalChannelFolder(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel folder.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>BaseItemDto.</returns>
        Task<BaseItemDto> GetChannelFolder(string userId, CancellationToken cancellationToken);

        /// <summary>
        /// Downloads the channel item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="destinationPath">The destination path.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task DownloadChannelItem(BaseItem item, string destinationPath, IProgress<double> progress, CancellationToken cancellationToken);
    }
}
