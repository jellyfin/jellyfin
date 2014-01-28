using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using System;
using System.Collections.Generic;
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

        public ItemImageProvider(ILogger logger, IProviderManager providerManager, IServerConfigurationManager config)
        {
            _logger = logger;
            _providerManager = providerManager;
            _config = config;
        }

        public bool ValidateImages(IHasImages item, IEnumerable<IImageProvider> providers)
        {
            var hasChanges = item.ValidateImages();

            foreach (var provider in providers.OfType<IImageFileProvider>())
            {
                var images = provider.GetImages(item);

                if (MergeImages(item, images))
                {
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        public async Task<RefreshResult> RefreshImages(IHasImages item, IEnumerable<IImageProvider> imageProviders, ImageRefreshOptions options, CancellationToken cancellationToken)
        {
            var result = new RefreshResult { UpdateType = ItemUpdateType.Unspecified };

            var providers = GetImageProviders(item, imageProviders).ToList();

            foreach (var provider in providers.OfType<IRemoteImageProvider>())
            {
                await RefreshFromProvider(item, provider, options, result, cancellationToken).ConfigureAwait(false);
            }

            foreach (var provider in providers.OfType<IDynamicImageProvider>())
            {
                await RefreshFromProvider(item, provider, result, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>
        /// Refreshes from provider.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="result">The result.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task RefreshFromProvider(IHasImages item, IDynamicImageProvider provider, RefreshResult result, CancellationToken cancellationToken)
        {
            _logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

            try
            {
                var images = provider.GetImageInfos(item);

                foreach (var image in images)
                {
                    if (!item.HasImage(image.Type))
                    {
                        var imageSource = await provider.GetImage(item, image).ConfigureAwait(false);

                        // See if the provider returned an image path or a stream
                        if (!string.IsNullOrEmpty(imageSource.Path))
                        {
                            item.SetImagePath(image.Type, imageSource.Path);
                        }
                        else
                        {
                            var mimeType = "image/" + imageSource.Format.ToString().ToLower();

                            await _providerManager.SaveImage((BaseItem)item, imageSource.Stream, mimeType, image.Type, null, Guid.NewGuid().ToString(), cancellationToken).ConfigureAwait(false);
                        }

                        result.UpdateType = result.UpdateType | ItemUpdateType.ImageUpdate;
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
        /// <param name="item"></param>
        /// <param name="images"></param>
        /// <returns></returns>
        private bool ContainsImages(IHasImages item, List<ImageType> images)
        {
            if (_singularImages.Any(i => images.Contains(i) && !item.HasImage(i)))
            {
                return false;
            }

            if (images.Contains(ImageType.Backdrop) && item.BackdropImagePaths.Count < GetMaxBackdropCount(item))
            {
                return false;
            }

            if (images.Contains(ImageType.Screenshot))
            {
                var hasScreenshots = item as IHasScreenshots;
                if (hasScreenshots != null)
                {
                    if (hasScreenshots.ScreenshotImagePaths.Count < GetMaxBackdropCount(item))
                    {
                        return false;
                    }
                }
            } 
            
            return true;
        }

        /// <summary>
        /// Refreshes from provider.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="provider">The provider.</param>
        /// <param name="options">The options.</param>
        /// <param name="result">The result.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task RefreshFromProvider(IHasImages item, IRemoteImageProvider provider, ImageRefreshOptions options, RefreshResult result, CancellationToken cancellationToken)
        {
            try
            {
                // TODO: Also factor in IsConfiguredToDownloadImage
                if (ContainsImages(item, provider.GetSupportedImages(item).ToList()))
                {
                    return;
                }

                _logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);
                
                var images = await provider.GetAllImages(item, cancellationToken).ConfigureAwait(false);
                var list = images.ToList();

                foreach (var type in _singularImages)
                {
                    if (IsConfiguredToDownloadImage(item, type) && !item.HasImage(type))
                    {
                        await DownloadImage(item, provider, result, list, type, cancellationToken).ConfigureAwait(false);
                    }
                }

                await DownloadBackdrops(item, provider, result, list, cancellationToken).ConfigureAwait(false);

                var hasScreenshots = item as IHasScreenshots;
                if (hasScreenshots != null)
                {
                    await DownloadScreenshots(hasScreenshots, provider, result, list, cancellationToken).ConfigureAwait(false);
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
        /// Gets the image providers.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="imageProviders">The image providers.</param>
        /// <returns>IEnumerable{IImageProvider}.</returns>
        private IEnumerable<IImageProvider> GetImageProviders(IHasImages item, IEnumerable<IImageProvider> imageProviders)
        {
            var providers = imageProviders.Where(i =>
            {
                try
                {
                    return i.Supports(item);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in ImageProvider.Supports", ex, i.Name);

                    return false;
                }
            });

            if (!_config.Configuration.EnableInternetProviders)
            {
                providers = providers.Where(i => !(i is IRemoteImageProvider));
            }

            return providers.OrderBy(i => i.Order);
        }

        private bool MergeImages(IHasImages item, List<LocalImageInfo> images)
        {
            var changed = false;

            foreach (var type in _singularImages)
            {
                var image = images.FirstOrDefault(i => i.Type == type);

                if (image != null)
                {
                    var oldPath = item.GetImagePath(type);

                    item.SetImagePath(type, image.Path);

                    if (!string.Equals(oldPath, image.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        changed = true;
                    }
                }
            }

            // The change reporting will only be accurate at the count level
            // Improve this if/when needed
            var backdrops = images.Where(i => i.Type == ImageType.Backdrop).ToList();
            if (backdrops.Count > 0)
            {
                var oldCount = item.BackdropImagePaths.Count;

                item.BackdropImagePaths = item.BackdropImagePaths
                    .Concat(backdrops.Select(i => i.Path))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (oldCount != item.BackdropImagePaths.Count)
                {
                    changed = true;
                }
            }

            var hasScreenshots = item as IHasScreenshots;
            if (hasScreenshots != null)
            {
                var screenshots = images.Where(i => i.Type == ImageType.Screenshot).ToList();

                if (screenshots.Count > 0)
                {
                    var oldCount = hasScreenshots.ScreenshotImagePaths.Count;

                    hasScreenshots.ScreenshotImagePaths = hasScreenshots.ScreenshotImagePaths
                        .Concat(screenshots.Select(i => i.Path))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    if (oldCount != hasScreenshots.ScreenshotImagePaths.Count)
                    {
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private async Task DownloadImage(IHasImages item, IRemoteImageProvider provider, RefreshResult result, IEnumerable<RemoteImageInfo> images, ImageType type, CancellationToken cancellationToken)
        {
            foreach (var image in images.Where(i => i.Type == type))
            {
                var url = image.Url;

                try
                {
                    var response = await provider.GetImageResponse(url, cancellationToken).ConfigureAwait(false);

                    await _providerManager.SaveImage((BaseItem)item, response.Content, response.ContentType, type, null, url, cancellationToken).ConfigureAwait(false);

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

        private async Task DownloadBackdrops(IHasImages item, IRemoteImageProvider provider, RefreshResult result, IEnumerable<RemoteImageInfo> images, CancellationToken cancellationToken)
        {
            const ImageType imageType = ImageType.Backdrop;
            var maxCount = GetMaxBackdropCount(item);

            foreach (var image in images.Where(i => i.Type == imageType))
            {
                if (item.BackdropImagePaths.Count >= maxCount)
                {
                    break;
                }

                var url = image.Url;

                if (item.ContainsImageWithSourceUrl(url))
                {
                    continue;
                }

                try
                {
                    var response = await provider.GetImageResponse(url, cancellationToken).ConfigureAwait(false);

                    await _providerManager.SaveImage((BaseItem)item, response.Content, response.ContentType, imageType, null, url, cancellationToken).ConfigureAwait(false);
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

        private async Task DownloadScreenshots(IHasScreenshots item, IRemoteImageProvider provider, RefreshResult result, IEnumerable<RemoteImageInfo> images, CancellationToken cancellationToken)
        {
            const ImageType imageType = ImageType.Screenshot;
            var maxCount = GetMaxScreenshotCount(item);

            foreach (var image in images.Where(i => i.Type == imageType))
            {
                if (item.ScreenshotImagePaths.Count >= maxCount)
                {
                    break;
                }

                var url = image.Url;

                if (item.ContainsImageWithSourceUrl(url))
                {
                    continue;
                }

                try
                {
                    var response = await provider.GetImageResponse(url, cancellationToken).ConfigureAwait(false);

                    await _providerManager.SaveImage((BaseItem)item, response.Content, response.ContentType, imageType, null, url, cancellationToken).ConfigureAwait(false);
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

        private bool IsConfiguredToDownloadImage(IHasImages item, ImageType type)
        {
            return true;
        }

        private int GetMaxBackdropCount(IHasImages item)
        {
            return 1;
        }

        private int GetMaxScreenshotCount(IHasScreenshots item)
        {
            return 1;
        }
    }
}
