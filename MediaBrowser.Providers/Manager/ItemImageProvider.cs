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

        public async Task<RefreshResult> RefreshImages(IHasImages item, IEnumerable<IImageProvider> imageProviders, ImageRefreshOptions refreshOptions, MetadataOptions savedOptions, CancellationToken cancellationToken)
        {
            var result = new RefreshResult { UpdateType = ItemUpdateType.Unspecified };

            var providers = GetImageProviders(item, imageProviders).ToList();

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

                                var stream = _fileSystem.GetFileStream(response.Path, FileMode.Open, FileAccess.Read,
                                    FileShare.Read, true);

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

            if (images.Contains(ImageType.Backdrop) && item.BackdropImagePaths.Count < backdropLimit)
            {
                return false;
            }

            if (images.Contains(ImageType.Screenshot))
            {
                var hasScreenshots = item as IHasScreenshots;
                if (hasScreenshots != null)
                {
                    if (hasScreenshots.ScreenshotImagePaths.Count < screenshotLimit)
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
                
                var images = await provider.GetAllImages(item, cancellationToken).ConfigureAwait(false);
                var list = images.ToList();

                foreach (var type in _singularImages)
                {
                    if (savedOptions.IsEnabled(type) && !item.HasImage(type))
                    {
                        await DownloadImage(item, provider, result, list, type, cancellationToken).ConfigureAwait(false);
                    }
                }

                await DownloadBackdrops(item, backdropLimit, provider, result, list, cancellationToken).ConfigureAwait(false);

                var hasScreenshots = item as IHasScreenshots;
                if (hasScreenshots != null)
                {
                    await DownloadScreenshots(hasScreenshots, screenshotLimit, provider, result, list, cancellationToken).ConfigureAwait(false);
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
            var providers = imageProviders;

            if (!_config.Configuration.EnableInternetProviders)
            {
                providers = providers.Where(i => !(i is IRemoteImageProvider));
            }

            return providers;
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

                    await _providerManager.SaveImage((BaseItem)item, response.Content, response.ContentType, type, null, cancellationToken).ConfigureAwait(false);

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

        private async Task DownloadBackdrops(IHasImages item, int limit, IRemoteImageProvider provider, RefreshResult result, IEnumerable<RemoteImageInfo> images, CancellationToken cancellationToken)
        {
            const ImageType imageType = ImageType.Backdrop;

            foreach (var image in images.Where(i => i.Type == imageType))
            {
                if (item.BackdropImagePaths.Count >= limit)
                {
                    break;
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

        private async Task DownloadScreenshots(IHasScreenshots item, int limit, IRemoteImageProvider provider, RefreshResult result, IEnumerable<RemoteImageInfo> images, CancellationToken cancellationToken)
        {
            const ImageType imageType = ImageType.Screenshot;

            foreach (var image in images.Where(i => i.Type == imageType))
            {
                if (item.ScreenshotImagePaths.Count >= limit)
                {
                    break;
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
