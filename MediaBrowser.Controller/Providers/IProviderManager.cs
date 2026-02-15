#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Interface IProviderManager.
    /// </summary>
    public interface IProviderManager
    {
        event EventHandler<GenericEventArgs<BaseItem>> RefreshStarted;

        event EventHandler<GenericEventArgs<BaseItem>> RefreshCompleted;

        event EventHandler<GenericEventArgs<Tuple<BaseItem, double>>> RefreshProgress;

        /// <summary>
        /// Queues the refresh.
        /// </summary>
        /// <param name="itemId">Item ID.</param>
        /// <param name="options">MetadataRefreshOptions for operation.</param>
        /// <param name="priority">RefreshPriority for operation.</param>
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
        /// Saves the image by giving the image path on filesystem.
        /// This method will remove the image on the source path after saving it to the destination.
        /// </summary>
        /// <param name="item">Image to save.</param>
        /// <param name="source">Source of image.</param>
        /// <param name="mimeType">Mime type image.</param>
        /// <param name="type">Type of image.</param>
        /// <param name="imageIndex">Index of image.</param>
        /// <param name="saveLocallyWithMedia">Option to save locally.</param>
        /// <param name="cancellationToken">CancellationToken to use with operation.</param>
        /// <returns>Task.</returns>
        Task SaveImage(BaseItem item, string source, string mimeType, ImageType type, int? imageIndex, bool? saveLocallyWithMedia, CancellationToken cancellationToken);

        Task SaveImage(Stream source, string mimeType, string path);

        /// <summary>
        /// Adds the metadata providers.
        /// </summary>
        /// <param name="imageProviders">Image providers to use.</param>
        /// <param name="metadataServices">Metadata services to use.</param>
        /// <param name="metadataProviders">Metadata providers to use.</param>
        /// <param name="metadataSavers">Metadata savers to use.</param>
        /// <param name="externalIds">External IDs to use.</param>
        /// <param name="externalUrlProviders">The list of external url providers.</param>
        void AddParts(
            IEnumerable<IImageProvider> imageProviders,
            IEnumerable<IMetadataService> metadataServices,
            IEnumerable<IMetadataProvider> metadataProviders,
            IEnumerable<IMetadataSaver> metadataSavers,
            IEnumerable<IExternalId> externalIds,
            IEnumerable<IExternalUrlProvider> externalUrlProviders);

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
        /// Gets the image providers for the provided item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="refreshOptions">The image refresh options.</param>
        /// <returns>The image providers for the item.</returns>
        IEnumerable<IImageProvider> GetImageProviders(BaseItem item, ImageRefreshOptions refreshOptions);

        /// <summary>
        /// Gets the metadata providers for the provided item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="libraryOptions">The library options.</param>
        /// <typeparam name="T">The type of metadata provider.</typeparam>
        /// <returns>The metadata providers.</returns>
        IEnumerable<IMetadataProvider<T>> GetMetadataProviders<T>(BaseItem item, LibraryOptions libraryOptions)
            where T : BaseItem;

        /// <summary>
        /// Gets the metadata savers for the provided item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="libraryOptions">The library options.</param>
        /// <returns>The metadata savers.</returns>
        IEnumerable<IMetadataSaver> GetMetadataSavers(BaseItem item, LibraryOptions libraryOptions);

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
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task SaveMetadataAsync(BaseItem item, ItemUpdateType updateType);

        /// <summary>
        /// Saves the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <param name="savers">The metadata savers.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        Task SaveMetadataAsync(BaseItem item, ItemUpdateType updateType, IEnumerable<string> savers);

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

        HashSet<Guid> GetRefreshQueue();

        void OnRefreshStart(BaseItem item);

        void OnRefreshProgress(BaseItem item, double progress);

        void OnRefreshComplete(BaseItem item);

        double? GetRefreshProgress(Guid id);
    }
}
