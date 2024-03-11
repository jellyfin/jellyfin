#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Channels
{
    public interface IChannelManager
    {
        /// <summary>
        /// Gets the channel features.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>ChannelFeatures.</returns>
        ChannelFeatures GetChannelFeatures(Guid? id);

        /// <summary>
        /// Gets all channel features.
        /// </summary>
        /// <returns>IEnumerable{ChannelFeatures}.</returns>
        ChannelFeatures[] GetAllChannelFeatures();

        bool EnableMediaSourceDisplay(BaseItem item);

        bool CanDelete(BaseItem item);

        Task DeleteItem(BaseItem item);

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
        /// <returns>The channels.</returns>
        Task<QueryResult<Channel>> GetChannelsInternalAsync(ChannelQuery query);

        /// <summary>
        /// Gets the channels.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The channels.</returns>
        Task<QueryResult<BaseItemDto>> GetChannelsAsync(ChannelQuery query);

        /// <summary>
        /// Gets the latest channel items.
        /// </summary>
        /// <param name="query">The item query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The latest channels.</returns>
        Task<QueryResult<BaseItemDto>> GetLatestChannelItems(InternalItemsQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the latest channel items.
        /// </summary>
        /// <param name="query">The item query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The latest channels.</returns>
        Task<QueryResult<BaseItem>> GetLatestChannelItemsInternal(InternalItemsQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The channel items.</returns>
        Task<QueryResult<BaseItemDto>> GetChannelItems(InternalItemsQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel items.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="progress">The progress to report to.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The channel items.</returns>
        Task<QueryResult<BaseItem>> GetChannelItemsInternal(InternalItemsQuery query, IProgress<double> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the channel item media sources.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The item media sources.</returns>
        IEnumerable<MediaSourceInfo> GetStaticMediaSources(BaseItem item, CancellationToken cancellationToken);
    }
}
