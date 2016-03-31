using MediaBrowser.Common.Extensions;
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
using CommonIO;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.MediaInfo;

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
            var hasChanges = false;

            if (!(item is Photo))
            {
                var images = providers.OfType<ILocalImageFileProvider>()
                    .SelectMany(i => i.GetImages(item, directoryService))
                    .ToList();

                if (MergeImages(item, images))
                {
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        public async Task<RefreshResult> RefreshImages(IHasImages item, IEnumerable<IImageProvider> imageProviders, ImageRefreshOptions refreshOptions, MetadataOptions savedOptions, CancellationToken cancellationToken)
        {
            if (refreshOptions.IsReplacingImage(ImageType.Backdrop))
            {
                ClearImages(item, ImageType.Backdrop);
            }
            if (refreshOptions.IsReplacingImage(ImageType.Screenshot))
            {
                ClearImages(item, ImageType.Screenshot);
            }

            var result = new RefreshResult { UpdateType = ItemUpdateType.None };

            var providers = imageProviders.ToList();

            var providerIds = new List<Guid>();

            // In order to avoid duplicates, only download these if there are none already
            var backdropLimit = savedOptions.GetLimit(ImageType.Backdrop);
            var screenshotLimit = savedOptions.GetLimit(ImageType.Screenshot);
            var downloadedImages = new List<ImageType>();

            foreach (var provider in providers)
            {
                var remoteProvider = provider as IRemoteImageProvider;

                if (remoteProvider != null)
                {
                    await RefreshFromProvider(item, remoteProvider, refreshOptions, savedOptions, backdropLimit, screenshotLimit, downloadedImages, result, cancellationToken).ConfigureAwait(false);
                    providerIds.Add(provider.GetType().FullName.GetMD5());
                    continue;
                }

                var dynamicImageProvider = provider as IDynamicImageProvider;

                if (dynamicImageProvider != null)
                {
                    await RefreshFromProvider(item, dynamicImageProvider, refreshOptions, savedOptions, downloadedImages, result, cancellationToken).ConfigureAwait(false);
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
        /// <param name="refreshOptions">The refresh options.</param>
        /// <param name="savedOptions">The saved options.</param>
        /// <param name="downloadedImages">The downloaded images.</param>
        /// <param name="result">The result.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task RefreshFromProvider(IHasImages item,
            IDynamicImageProvider provider,
            ImageRefreshOptions refreshOptions,
            MetadataOptions savedOptions,
            ICollection<ImageType> downloadedImages,
            RefreshResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                var images = provider.GetSupportedImages(item);

                foreach (var imageType in images)
                {
                    if (!IsEnabled(savedOptions, imageType, item)) continue;

                    if (!HasImage(item, imageType) || (refreshOptions.IsReplacingImage(imageType) && !downloadedImages.Contains(imageType)))
                    {
                        _logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

                        var response = await provider.GetImage(item, imageType, cancellationToken).ConfigureAwait(false);

                        if (response.HasImage)
                        {
                            if (!string.IsNullOrEmpty(response.Path))
                            {
                                if (response.Protocol == MediaProtocol.Http)
                                {
                                    _logger.Debug("Setting image url into item {0}", item.Id);
                                    item.SetImage(new ItemImageInfo
                                    {
                                        Path = response.Path,
                                        Type = imageType

                                    }, 0);
                                }
                                else
                                {
                                    var mimeType = MimeTypes.GetMimeType(response.Path);

                                    var stream = _fileSystem.GetFileStream(response.Path, FileMode.Open, FileAccess.Read, FileShare.Read, true);

                                    await _providerManager.SaveImage(item, stream, mimeType, imageType, null, response.InternalCacheKey, cancellationToken).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                var mimeType = "image/" + response.Format.ToString().ToLower();

                                await _providerManager.SaveImage(item, response.Stream, mimeType, imageType, null, response.InternalCacheKey, cancellationToken).ConfigureAwait(false);
                            }

                            downloadedImages.Add(imageType);
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

        private bool HasImage(IHasImages item, ImageType type)
        {
            var image = item.GetImageInfo(type, 0);

            // if it's a placeholder image then pretend like it's not there so that we can replace it
            return image != null && !image.IsPlaceholder;
        }

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
            if (_singularImages.Any(i => images.Contains(i) && !HasImage(item, i) && savedOptions.GetLimit(i) > 0))
            {
                return false;
            }

            if (images.Contains(ImageType.Backdrop) && item.GetImages(ImageType.Backdrop).Count() < backdropLimit)
            {
                return false;
            }

            if (images.Contains(ImageType.Screenshot) && item.GetImages(ImageType.Screenshot).Count() < screenshotLimit)
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
        /// <param name="downloadedImages">The downloaded images.</param>
        /// <param name="result">The result.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task RefreshFromProvider(IHasImages item,
            IRemoteImageProvider provider,
            ImageRefreshOptions refreshOptions,
            MetadataOptions savedOptions,
            int backdropLimit,
            int screenshotLimit,
            ICollection<ImageType> downloadedImages,
            RefreshResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                if (!item.SupportsRemoteImageDownloading)
                {
                    return;
                }

                if (!refreshOptions.ReplaceAllImages &&
                    refreshOptions.ReplaceImages.Count == 0 &&
                    ContainsImages(item, provider.GetSupportedImages(item).ToList(), savedOptions, backdropLimit, screenshotLimit))
                {
                    return;
                }

                _logger.Debug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

                var images = await _providerManager.GetAvailableRemoteImages(item, new RemoteImageQuery
                {
                    ProviderName = provider.Name,
                    IncludeAllLanguages = false,
                    IncludeDisabledProviders = false,

                }, cancellationToken).ConfigureAwait(false);

                var list = images.ToList();
                int minWidth;

                foreach (var imageType in _singularImages)
                {
                    if (!IsEnabled(savedOptions, imageType, item)) continue;

                    if (!HasImage(item, imageType) || (refreshOptions.IsReplacingImage(imageType) && !downloadedImages.Contains(imageType)))
                    {
                        minWidth = savedOptions.GetMinWidth(imageType);
                        var downloaded = await DownloadImage(item, provider, result, list, minWidth, imageType, cancellationToken).ConfigureAwait(false);

                        if (downloaded)
                        {
                            downloadedImages.Add(imageType);
                        }
                    }
                }

                if (!item.LockedFields.Contains(MetadataFields.Backdrops))
                {
                    minWidth = savedOptions.GetMinWidth(ImageType.Backdrop);
                    await DownloadBackdrops(item, ImageType.Backdrop, backdropLimit, provider, result, list, minWidth, cancellationToken).ConfigureAwait(false);
                }

                if (!item.LockedFields.Contains(MetadataFields.Screenshots))
                {
                    var hasScreenshots = item as IHasScreenshots;
                    if (hasScreenshots != null)
                    {
                        minWidth = savedOptions.GetMinWidth(ImageType.Screenshot);
                        await DownloadBackdrops(item, ImageType.Screenshot, screenshotLimit, provider, result, list, minWidth, cancellationToken).ConfigureAwait(false);
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
                _logger.ErrorException("Error in {0}", ex, provider.Name);
            }
        }

        private bool IsEnabled(MetadataOptions options, ImageType type, IHasImages item)
        {
            if (type == ImageType.Backdrop)
            {
                if (item.LockedFields.Contains(MetadataFields.Backdrops))
                {
                    return false;
                }
            }
            else if (type == ImageType.Screenshot)
            {
                if (item.LockedFields.Contains(MetadataFields.Screenshots))
                {
                    return false;
                }
            }
            else
            {
                if (item.LockedFields.Contains(MetadataFields.Images))
                {
                    return false;
                }
            }

            return options.IsEnabled(type);
        }

        private void ClearImages(IHasImages item, ImageType type)
        {
            var deleted = false;
            var deletedImages = new List<ItemImageInfo>();

            foreach (var image in item.GetImages(type).ToList())
            {
                if (!image.IsLocalFile)
                {
                    deletedImages.Add(image);
                    continue;
                }

                // Delete the source file
                var currentFile = new FileInfo(image.Path);

                // Deletion will fail if the file is hidden so remove the attribute first
                if (currentFile.Exists)
                {
                    if ((currentFile.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                    {
                        currentFile.Attributes &= ~FileAttributes.Hidden;
                    }

                    _fileSystem.DeleteFile(currentFile.FullName);
                    deleted = true;
                }
            }

            foreach (var image in deletedImages)
            {
                item.RemoveImage(image);
            }

            if (deleted)
            {
                item.ValidateImages(new DirectoryService(_logger, _fileSystem));
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

                    if (currentImage == null)
                    {
                        item.SetImagePath(type, image.FileInfo);
                        changed = true;
                    }
                    else if (!string.Equals(currentImage.Path, image.FileInfo.FullName, StringComparison.OrdinalIgnoreCase))
                    {
                        item.SetImagePath(type, image.FileInfo);
                        changed = true;
                    }
                    else
                    {
                        currentImage.DateModified = _fileSystem.GetLastWriteTimeUtc(image.FileInfo);
                    }
                }
                else
                {
                    var existing = item.GetImageInfo(type, 0);
                    if (existing != null)
                    {
                        if (existing.IsLocalFile && !_fileSystem.FileExists(existing.Path))
                        {
                            item.RemoveImage(existing);
                            changed = true;
                        }
                    }
                }
            }

            if (UpdateMultiImages(item, images, ImageType.Backdrop))
            {
                changed = true;
            }

            var hasScreenshots = item as IHasScreenshots;
            if (hasScreenshots != null)
            {
                if (UpdateMultiImages(item, images, ImageType.Screenshot))
                {
                    changed = true;
                }
            }

            return changed;
        }

        private bool UpdateMultiImages(IHasImages item, List<LocalImageInfo> images, ImageType type)
        {
            var changed = false;

            var newImages = images.Where(i => i.Type == type).ToList();

            var newImageFileInfos = newImages
                    .Select(i => i.FileInfo)
                    .ToList();

            if (item.AddImages(type, newImageFileInfos))
            {
                changed = true;
            }

            return changed;
        }

        private async Task<bool> DownloadImage(IHasImages item,
            IRemoteImageProvider provider,
            RefreshResult result,
            IEnumerable<RemoteImageInfo> images,
            int minWidth,
            ImageType type,
            CancellationToken cancellationToken)
        {
            var eligibleImages = images
                .Where(i => i.Type == type && !(i.Width.HasValue && i.Width.Value < minWidth))
                .ToList();

            if (EnableImageStub(item, type) && eligibleImages.Count > 0)
            {
                SaveImageStub(item, type, eligibleImages.Select(i => i.Url));
                result.UpdateType = result.UpdateType | ItemUpdateType.ImageUpdate;
                return true;
            }

            foreach (var image in eligibleImages)
            {
                var url = image.Url;

                try
                {
                    var response = await provider.GetImageResponse(url, cancellationToken).ConfigureAwait(false);

                    await _providerManager.SaveImage(item, response.Content, response.ContentType, type, null, cancellationToken).ConfigureAwait(false);

                    result.UpdateType = result.UpdateType | ItemUpdateType.ImageUpdate;
                    return true;
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

            return false;
        }

        private bool EnableImageStub(IHasImages item, ImageType type)
        {
            if (item is LiveTvProgram)
            {
                return true;
            }

            if (_config.Configuration.DownloadImagesInAdvance)
            {
                return false;
            }

            if (item.LocationType == LocationType.Remote || item.LocationType == LocationType.Virtual)
            {
                return true;
            }

            if (!item.IsSaveLocalMetadataEnabled())
            {
                return true;
            }

            if (item is IItemByName && !(item is MusicArtist))
            {
                var hasDualAccess = item as IHasDualAccess;
                if (hasDualAccess == null || hasDualAccess.IsAccessedByName)
                {
                    return true;
                }
            }

            switch (type)
            {
                case ImageType.Primary:
                    return false;
                case ImageType.Thumb:
                    return false;
                case ImageType.Logo:
                    return false;
                case ImageType.Backdrop:
                    return false;
                case ImageType.Screenshot:
                    return false;
                default:
                    return true;
            }
        }

        private void SaveImageStub(IHasImages item, ImageType imageType, IEnumerable<string> urls)
        {
            var newIndex = item.AllowsMultipleImages(imageType) ? item.GetImages(imageType).Count() : 0;

            SaveImageStub(item, imageType, urls, newIndex);
        }

        private void SaveImageStub(IHasImages item, ImageType imageType, IEnumerable<string> urls, int newIndex)
        {
            var path = string.Join("|", urls.Take(1).ToArray());

            item.SetImage(new ItemImageInfo
            {
                Path = path,
                Type = imageType

            }, newIndex);
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

                if (EnableImageStub(item, imageType))
                {
                    SaveImageStub(item, imageType, new[] { url });
                    result.UpdateType = result.UpdateType | ItemUpdateType.ImageUpdate;
                    continue;
                }

                try
                {
                    var response = await provider.GetImageResponse(url, cancellationToken).ConfigureAwait(false);

                    // If there's already an image of the same size, skip it
                    if (response.ContentLength.HasValue)
                    {
                        try
                        {
                            if (item.GetImages(imageType).Any(i => new FileInfo(i.Path).Length == response.ContentLength.Value))
                            {
                                response.Content.Dispose();
                                continue;
                            }
                        }
                        catch (IOException ex)
                        {
                            _logger.ErrorException("Error examining images", ex);
                        }
                    }

                    await _providerManager.SaveImage(item, response.Content, response.ContentType, imageType, null, cancellationToken).ConfigureAwait(false);
                    result.UpdateType = result.UpdateType | ItemUpdateType.ImageUpdate;
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