using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Interface IProviderManager
    /// </summary>
    public interface IProviderManager
    {
        /// <summary>
        /// Queues the refresh.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="options">The options.</param>
        void QueueRefresh(Guid itemId, MetadataRefreshOptions options);

        /// <summary>
        /// Refreshes the full item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task RefreshFullItem(IHasMetadata item, MetadataRefreshOptions options, CancellationToken cancellationToken);
        
        /// <summary>
        /// Refreshes the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task<ItemUpdateType> RefreshSingleItem(IHasMetadata item, MetadataRefreshOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="url">The URL.</param>
        /// <param name="resourcePool">The resource pool.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveImage(IHasImages item, string url, SemaphoreSlim resourcePool, ImageType type, int? imageIndex, CancellationToken cancellationToken);

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
        Task SaveImage(IHasImages item, Stream source, string mimeType, ImageType type, int? imageIndex, CancellationToken cancellationToken);

        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="source">The source.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="internalCacheKey">The internal cache key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveImage(IHasImages item, Stream source, string mimeType, ImageType type, int? imageIndex, string internalCacheKey, CancellationToken cancellationToken);

        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="source">The source.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="internalCacheKey">The internal cache key.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveImage(IHasImages item, string source, string mimeType, ImageType type, int? imageIndex, string internalCacheKey, CancellationToken cancellationToken);
        
        /// <summary>
        /// Adds the metadata providers.
        /// </summary>
        /// <param name="imageProviders">The image providers.</param>
        /// <param name="metadataServices">The metadata services.</param>
        /// <param name="metadataProviders">The metadata providers.</param>
        /// <param name="savers">The savers.</param>
        /// <param name="imageSavers">The image savers.</param>
        /// <param name="externalIds">The external ids.</param>
        void AddParts(IEnumerable<IImageProvider> imageProviders, IEnumerable<IMetadataService> metadataServices, IEnumerable<IMetadataProvider> metadataProviders,
            IEnumerable<IMetadataSaver> savers,
            IEnumerable<IImageSaver> imageSavers,
            IEnumerable<IExternalId> externalIds);

        /// <summary>
        /// Gets the available remote images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{RemoteImageInfo}}.</returns>
        Task<IEnumerable<RemoteImageInfo>> GetAvailableRemoteImages(IHasImages item, RemoteImageQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the image providers.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{ImageProviderInfo}.</returns>
        IEnumerable<ImageProviderInfo> GetRemoteImageProviderInfo(IHasImages item);

        /// <summary>
        /// Gets all metadata plugins.
        /// </summary>
        /// <returns>IEnumerable{MetadataPlugin}.</returns>
        IEnumerable<MetadataPluginSummary> GetAllMetadataPlugins();

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
        Task SaveMetadata(IHasMetadata item, ItemUpdateType updateType);

        /// <summary>
        /// Saves the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <param name="savers">The savers.</param>
        /// <returns>Task.</returns>
        Task SaveMetadata(IHasMetadata item, ItemUpdateType updateType, IEnumerable<string> savers);
        
        /// <summary>
        /// Gets the metadata options.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>MetadataOptions.</returns>
        MetadataOptions GetMetadataOptions(IHasImages item);

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
    }
}