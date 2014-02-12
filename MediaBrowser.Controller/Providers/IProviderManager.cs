using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
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
        /// Refreshes the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task RefreshMetadata(IHasMetadata item, MetadataRefreshOptions options, CancellationToken cancellationToken);

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
        Task SaveImage(BaseItem item, string url, SemaphoreSlim resourcePool, ImageType type, int? imageIndex, CancellationToken cancellationToken);

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
        /// Adds the metadata providers.
        /// </summary>
        /// <param name="imageProviders">The image providers.</param>
        /// <param name="metadataServices">The metadata services.</param>
        /// <param name="metadataProviders">The metadata providers.</param>
        /// <param name="savers">The savers.</param>
        void AddParts(IEnumerable<IImageProvider> imageProviders, IEnumerable<IMetadataService> metadataServices, IEnumerable<IMetadataProvider> metadataProviders,
            IEnumerable<IMetadataSaver> savers);

        /// <summary>
        /// Gets the available remote images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="providerName">Name of the provider.</param>
        /// <param name="type">The type.</param>
        /// <returns>Task{IEnumerable{RemoteImageInfo}}.</returns>
        Task<IEnumerable<RemoteImageInfo>> GetAvailableRemoteImages(IHasImages item, CancellationToken cancellationToken, string providerName = null, ImageType? type = null);

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
        /// Saves the metadata.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="updateType">Type of the update.</param>
        /// <returns>Task.</returns>
        Task SaveMetadata(IHasMetadata item, ItemUpdateType updateType);

        /// <summary>
        /// Gets the metadata options.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>MetadataOptions.</returns>
        MetadataOptions GetMetadataOptions(IHasImages item);
    }
}