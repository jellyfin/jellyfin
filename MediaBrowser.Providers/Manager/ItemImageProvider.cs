using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Manager
{
    public class ItemImageProvider
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        public ItemImageProvider(ILogger logger, IProviderManager providerManager, IServerConfigurationManager config, IFileSystem fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
            _config = config;
            _fileSystem = fileSystem;
        }

        public bool ValidateImages(IHasImages item, IEnumerable<IImageProvider> providers, IDirectoryService directoryService)
        {
            var hasChanges = item.ValidateImages(directoryService);

            foreach (var provider in providers.OfType<ILocalImageFileProvider>())
            {
                var images = provider.GetImages(item, directoryService);

                if (MergeImages(item, images))
                {
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        public async Task<RefreshResult> RefreshImages(IHasImages item, IEnumerable<IImageProvider> imageProviders, ImageRefreshOptions refreshOptions, MetadataOptions savedOptions, CancellationToken cancellationToken)
        {
            var result = new RefreshResult { UpdateType = ItemUpdateType.None };

            var providers = imageProviders.ToList();

            var providerIds = new List<Guid>();

            // In order to avoid duplicates, only download these if there are none already
            var backdropLimit = item.HasImage(ImageType.Backdrop) ? 0 : savedOptions.GetLimit(ImageType.Backdrop);
            var screenshotLimit = item.HasImage(ImageType.Screenshot) ? 0 : savedOptions.GetLimit(ImageType.Screenshot);

            foreach (var provider in providers)
            {
                var remoteProvider = provider as IRemoteImageProvider;

                if (remoteProvider != null)
                {
                    await RefreshFromProvider(item, remoteProvider, refreshOptions, savedOptions, backdropLimit, screenshotLimit, result, cancellationToken).ConfigureAwait(false);
                    providerIds.Add(provider.GetType().FullName.GetMD5());
                    continue;
                }

                var dynamicImageProvider = provider as IDynamicImageProvider;

                if (dynamicImageProvider != null)
                {
                    await RefreshFromProvider(item, dynamicImageProvider, savedOptions, result, cancellationToken).ConfigureAwait(false);
                    providerIds.Add(provider.GetType().FullName.GetMD5());
                }
            }

            result.Providers = providerIds;

            return result;
        }

        /// <summary>
        /// Refreshes from provider.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="savedOptions">The saved options.</param>
        /// <param name="result">The result.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task RefreshFromProvider(IHasImages item, IDynamicImageProvider provider, MetadataOptions savedOptions, RefreshResult result, CancellationToken cancellationToken)
        {
            try
            {
                var images = provider.GetSupportedImages(item);

                foreach (var imageType in images)
                {
                    if (!item.HasImage(imageType) && savedOptions.IsEnabled(imageType))
                    {
                        _logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

                        var response = await provider.GetImage(item, imageType, cancellationToken).ConfigureAwait(false);

                        if (response.HasImage)
                        {
                            if (!string.IsNullOrEmpty(response.Path))
                            {
                                var mimeType = "image/" + Path.GetExtension(response.Path).TrimStart('.').ToLower();

                                var stream = _fileSystem.GetFileStream(response.Path, FileMode.Open, FileAccess.Read, FileShare.Read, true);

                                await _providerManager.SaveImage((BaseItem)item, stream, mimeType, imageType, null, cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                var mimeType = "image/" + response.Format.ToString().ToLower();

                                await _providerManager.SaveImage((BaseItem)item, response.Stream, mimeType, imageType, null, cancellationToken).ConfigureAwait(false);
                            }

                            result.UpdateType = result.UpdateType | ItemUpdateType.ImageUpdate;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.Status = ProviderRefreshStatus.CompletedWithErrors;
                _logger.ErrorException("Error in {0}", ex, provider.Name);
            }
        }

        /// <summary>
        /// Image types that are only one per item
        /// </summary>
        private readonly ImageType[] _singularImages =
        {
            ImageType.Primary,
            ImageType.Art,
            ImageType.Banner,
            ImageType.Box,
            ImageType.BoxRear,
            ImageType.Disc,
            ImageType.Logo,
            ImageType.Menu,
            ImageType.Thumb
        };

        /// <summary>
        /// Determines if an item already contains the given images
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="images">The images.</param>
        /// <param name="savedOptions">The saved options.</param>
        /// <param name="backdropLimit">The backdrop limit.</param>
        /// <param name="screenshotLimit">The screenshot limit.</param>
        /// <returns><c>true</c> if the specified item contains images; otherwise, <c>false</c>.</returns>
        private bool ContainsImages(IHasImages item, List<ImageType> images, MetadataOptions savedOptions, int backdropLimit, int screenshotLimit)
        {
            if (_singularImages.Any(i => images.Contains(i) && !item.HasImage(i) && savedOptions.GetLimit(i) > 0))
            {
                return false;
            }

            if (images.Contains(ImageType.Backdrop) && item.GetImages(ImageType.Backdrop).Count() < backdropLimit)
            {
                return false;
            }

            if (images.Contains(ImageType.Screenshot) && item.GetImages(ImageType.Screenshot).Count() < backdropLimit)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Refreshes from provider.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="refreshOptions">The refresh options.</param>
        /// <param name="savedOptions">The saved options.</param>
        /// <param name="backdropLimit">The backdrop limit.</param>
        /// <param name="screenshotLimit">The screenshot limit.</param>
        /// <param name="result">The result.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task RefreshFromProvider(IHasImages item, IRemoteImageProvider provider, ImageRefreshOptions refreshOptions, MetadataOptions savedOptions, int backdropLimit, int screenshotLimit, RefreshResult result, CancellationToken cancellationToken)
        {
            try
            {
                if (ContainsImages(item, provider.GetSupportedImages(item).ToList(), savedOptions, backdropLimit, screenshotLimit))
                {
                    return;
                }

                _logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

                var images = await _providerManager.GetAvailableRemoteImages(item, cancellationToken, provider.Name).ConfigureAwait(false);
                var list = images.ToList();
                int minWidth;

                foreach (var type in _singularImages)
                {
                    if (savedOptions.IsEnabled(type) && !item.HasImage(type))
                    {
                        minWidth = savedOptions.GetMinWidth(type);
                        await DownloadImage(item, provider, result, list, minWidth, type, cancellationToken).ConfigureAwait(false);
                    }
                }

                minWidth = savedOptions.GetMinWidth(ImageType.Backdrop);
                await DownloadBackdrops(item, ImageType.Backdrop, backdropLimit, provider, result, list, minWidth, cancellationToken).ConfigureAwait(false);

                var hasScreenshots = item as IHasScreenshots;
                if (hasScreenshots != null)
                {
                    minWidth = savedOptions.GetMinWidth(ImageType.Screenshot);
                    await DownloadBackdrops(item, ImageType.Screenshot, screenshotLimit, provider, result, list, minWidth, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.Status = ProviderRefreshStatus.CompletedWithErrors;
                _logger.ErrorException("Error in {0}", ex, provider.Name);
            }
        }

        public bool MergeImages(IHasImages item, List<LocalImageInfo> images)
        {
            var changed = false;

            foreach (var type in _singularImages)
            {
                var image = images.FirstOrDefault(i => i.Type == type);

                if (image != null)
                {
                    var currentImage = item.GetImageInfo(type, 0);

                    if (currentImage == null || !string.Equals(currentImage.Path, image.FileInfo.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        item.SetImagePath(type, image.FileInfo);
                        changed = true;
                    }
                }
            }

            var backdrops = images.Where(i => i.Type == ImageType.Backdrop).ToList();
            if (backdrops.Count > 0)
            {
                var foundImages = images.Where(i => i.Type == ImageType.Backdrop)
                    .Select(i => i.FileInfo)
                    .ToList();

                if (foundImages.Count > 0)
                {
                    if (item.AddImages(ImageType.Backdrop, foundImages))
                    {
                        changed = true;
                    }
                }
            }

            var hasScreenshots = item as IHasScreenshots;
            if (hasScreenshots != null)
            {
                var foundImages = images.Where(i => i.Type == ImageType.Screenshot)
                    .Select(i => i.FileInfo)
                    .ToList();

                if (foundImages.Count > 0)
                {
                    if (item.AddImages(ImageType.Screenshot, foundImages))
                    {
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private async Task DownloadImage(IHasImages item, IRemoteImageProvider provider, RefreshResult result, IEnumerable<RemoteImageInfo> images, int minWidth, ImageType type, CancellationToken cancellationToken)
        {
            foreach (var image in images.Where(i => i.Type == type))
            {
                if (image.Width.HasValue && image.Width.Value < minWidth)
                {
                    continue;
                }

                var url = image.Url;

                try
                {
                    var response = await provider.GetImageResponse(url, cancellationToken).ConfigureAwait(false);

                    await _providerManager.SaveImage((BaseItem)item, response.Content, response.ContentType, type, null, cancellationToken).ConfigureAwait(false);

                    result.UpdateType = result.UpdateType | ItemUpdateType.ImageUpdate;
                    break;
                }
                catch (HttpException ex)
                {
                    // Sometimes providers send back bad url's. Just move to the next image
                    if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.NotFound)
                    {
                        continue;
                    }
                    break;
                }
            }
        }

        private async Task DownloadBackdrops(IHasImages item, ImageType imageType, int limit, IRemoteImageProvider provider, RefreshResult result, IEnumerable<RemoteImageInfo> images, int minWidth, CancellationToken cancellationToken)
        {
            foreach (var image in images.Where(i => i.Type == imageType))
            {
                if (item.GetImages(imageType).Count() >= limit)
                {
                    break;
                }

                if (image.Width.HasValue && image.Width.Value < minWidth)
                {
                    continue;
                }

                var url = image.Url;

                try
                {
                    var response = await provider.GetImageResponse(url, cancellationToken).ConfigureAwait(false);

                    await _providerManager.SaveImage((BaseItem)item, response.Content, response.ContentType, imageType, null, cancellationToken).ConfigureAwait(false);
                    result.UpdateType = result.UpdateType | ItemUpdateType.ImageUpdate;
                    break;
                }
                catch (HttpException ex)
                {
                    // Sometimes providers send back bad url's. Just move onto the next image
                    if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.NotFound)
                    {
                        continue;
                    }
                    break;
                }
            }
        }
    }
}
