using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Interface IProviderManager.
    /// </summary>
    public interface IProviderManager
    {
        /// <summary>
        /// Queues the refresh.
        /// </summary>
        void QueueRefresh(Guid itemId, MetadataRefreshOptions options, RefreshPriority priority);

        /// <summary>
        /// Refreshes the full item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task RefreshFullItem(BaseItem item, MetadataRefreshOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Refreshes the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task<ItemUpdateType> RefreshSingleItem(BaseItem item, MetadataRefreshOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="url">The URL.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveImage(BaseItem item, string url, ImageType type, int? imageIndex, CancellationToken cancellationToken);

        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="source">The source.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveImage(BaseItem item, Stream source, string mimeType, ImageType type, int? imageIndex, CancellationToken cancellationToken);

        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <returns>Task.</returns>
        Task SaveImage(BaseItem item, string source, string mimeType, ImageType type, int? imageIndex, bool? saveLocallyWithMedia, CancellationToken cancellationToken);

        /// <summary>
        /// Adds the metadata providers.
        /// </summary>
        void AddParts(IEnumerable<IImageProvider> imageProviders, IEnumerable<IMetadataService> metadataServices, IEnumerable<IMetadataProvider> metadataProviders,
            IEnumerable<IMetadataSaver> savers,
            IEnumerable<IExternalId> externalIds);

        /// <summary>
        /// Gets the available remote images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RemoteImageInfo}}.</returns>
        Task<IEnumerable<RemoteImageInfo>> GetAvailableRemoteImages(BaseItem item, RemoteImageQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the image providers.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{ImageProviderInfo}.</returns>
        IEnumerable<ImageProviderInfo> GetRemoteImageProviderInfo(BaseItem item);

        /// <summary>
        /// Gets all metadata plugins.
        /// </summary>
        /// <returns>IEnumerable{MetadataPlugin}.</returns>
        MetadataPluginSummary[] GetAllMetadataPlugins();

        /// <summary>
        /// Gets the external urls.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{ExternalUrl}.</returns>
        IEnumerable<ExternalUrl> GetExternalUrls(BaseItem item);

        /// <summary>
        /// Gets the external identifier infos.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{ExternalIdInfo}.</returns>
        IEnumerable<ExternalIdInfo> GetExternalIdInfos(IHasProviderIds item);

        /// <summary>
        /// Saves the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns>Task.</returns>
        void SaveMetadata(BaseItem item, ItemUpdateType updateType);

        /// <summary>
        /// Saves the metadata.
        /// </summary>
        void SaveMetadata(BaseItem item, ItemUpdateType updateType, IEnumerable<string> savers);

        /// <summary>
        /// Gets the metadata options.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>MetadataOptions.</returns>
        MetadataOptions GetMetadataOptions(BaseItem item);

        /// <summary>
        /// Gets the remote search results.
        /// </summary>
        /// <typeparam name="TItemType">The type of the t item type.</typeparam>
        /// <typeparam name="TLookupType">The type of the t lookup type.</typeparam>
        /// <param name="searchInfo">The search information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{SearchResult{``1}}}.</returns>
        Task<IEnumerable<RemoteSearchResult>> GetRemoteSearchResults<TItemType, TLookupType>(
            RemoteSearchQuery<TLookupType> searchInfo,
            CancellationToken cancellationToken)
            where TItemType : BaseItem, new()
            where TLookupType : ItemLookupInfo;

        /// <summary>
        /// Gets the search image.
        /// </summary>
        /// <param name="providerName">Name of the provider.</param>
        /// <param name="url">The URL.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{HttpResponseInfo}.</returns>
        Task<HttpResponseInfo> GetSearchImage(string providerName, string url, CancellationToken cancellationToken);

        Dictionary<Guid, Guid> GetRefreshQueue();

        void OnRefreshStart(BaseItem item);

        void OnRefreshProgress(BaseItem item, double progress);

        void OnRefreshComplete(BaseItem item);

        double? GetRefreshProgress(Guid id);

        event EventHandler<GenericEventArgs<BaseItem>> RefreshStarted;

        event EventHandler<GenericEventArgs<BaseItem>> RefreshCompleted;

        event EventHandler<GenericEventArgs<Tuple<BaseItem, double>>> RefreshProgress;
    }

    public enum RefreshPriority
    {
        High = 0,
        Normal = 1,
        Low = 2
    }
}
