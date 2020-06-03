using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Manager
{
    public class ItemImageProvider
    {
        private readonly ILogger _logger;
        private readonly IProviderManager _providerManager;
        private readonly IFileSystem _fileSystem;

        public ItemImageProvider(ILogger logger, IProviderManager providerManager, IFileSystem fileSystem)
        {
            _logger = logger;
            _providerManager = providerManager;
            _fileSystem = fileSystem;
        }

        public bool ValidateImages(BaseItem item, IEnumerable<IImageProvider> providers, IDirectoryService directoryService)
        {
            var hasChanges = false;

            if (!(item is Photo))
            {
                var images = providers.OfType<ILocalImageProvider>()
                    .SelectMany(i => i.GetImages(item, directoryService))
                    .ToList();

                if (MergeImages(item, images))
                {
                    hasChanges = true;
                }
            }

            return hasChanges;
        }

        public async Task<RefreshResult> RefreshImages(BaseItem item, LibraryOptions libraryOptions, List<IImageProvider> providers, ImageRefreshOptions refreshOptions, MetadataOptions savedOptions, CancellationToken cancellationToken)
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

            var typeName = item.GetType().Name;
            var typeOptions = libraryOptions.GetTypeOptions(typeName) ?? new TypeOptions { Type = typeName };

            // In order to avoid duplicates, only download these if there are none already
            var backdropLimit = typeOptions.GetLimit(ImageType.Backdrop);
            var screenshotLimit = typeOptions.GetLimit(ImageType.Screenshot);
            var downloadedImages = new List<ImageType>();

            foreach (var provider in providers)
            {
                var remoteProvider = provider as IRemoteImageProvider;

                if (remoteProvider != null)
                {
                    await RefreshFromProvider(item, libraryOptions, remoteProvider, refreshOptions, typeOptions, backdropLimit, screenshotLimit, downloadedImages, result, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                var dynamicImageProvider = provider as IDynamicImageProvider;

                if (dynamicImageProvider != null)
                {
                    await RefreshFromProvider(item, dynamicImageProvider, refreshOptions, typeOptions, libraryOptions, downloadedImages, result, cancellationToken).ConfigureAwait(false);
                }
            }

            return result;
        }

        /// <summary>
        /// Refreshes from provider.
        /// </summary>
        private async Task RefreshFromProvider(BaseItem item,
            IDynamicImageProvider provider,
            ImageRefreshOptions refreshOptions,
            TypeOptions savedOptions,
            LibraryOptions libraryOptions,
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
                        _logger.LogDebug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

                        var response = await provider.GetImage(item, imageType, cancellationToken).ConfigureAwait(false);

                        if (response.HasImage)
                        {
                            if (!string.IsNullOrEmpty(response.Path))
                            {
                                if (response.Protocol == MediaProtocol.Http)
                                {
                                    _logger.LogDebug("Setting image url into item {0}", item.Id);
                                    item.SetImage(new ItemImageInfo
                                    {
                                        Path = response.Path,
                                        Type = imageType

                                    }, 0);
                                }
                                else
                                {
                                    var mimeType = MimeTypes.GetMimeType(response.Path);

                                    var stream = new FileStream(response.Path, FileMode.Open, FileAccess.Read, FileShare.Read, IODefaults.FileStreamBufferSize, true);

                                    await _providerManager.SaveImage(item, stream, mimeType, imageType, null, cancellationToken).ConfigureAwait(false);
                                }
                            }
                            else
                            {
                                var mimeType = "image/" + response.Format.ToString().ToLowerInvariant();

                                await _providerManager.SaveImage(item, response.Stream, mimeType, imageType, null, cancellationToken).ConfigureAwait(false);
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
                _logger.LogError(ex, "Error in {provider}", provider.Name);
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

        private bool HasImage(BaseItem item, ImageType type)
        {
            return item.HasImage(type);
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
        private bool ContainsImages(BaseItem item, List<ImageType> images, TypeOptions savedOptions, int backdropLimit, int screenshotLimit)
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
        private async Task RefreshFromProvider(
            BaseItem item,
            LibraryOptions libraryOptions,
            IRemoteImageProvider provider,
            ImageRefreshOptions refreshOptions,
            TypeOptions savedOptions,
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
                    refreshOptions.ReplaceImages.Length == 0 &&
                    ContainsImages(item, provider.GetSupportedImages(item).ToList(), savedOptions, backdropLimit, screenshotLimit))
                {
                    return;
                }

                _logger.LogDebug("Running {0} for {1}", provider.GetType().Name, item.Path ?? item.Name);

                var images = await _providerManager.GetAvailableRemoteImages(
                    item,
                    new RemoteImageQuery(provider.Name)
                    {
                        IncludeAllLanguages = false,
                        IncludeDisabledProviders = false,
                    },
                    cancellationToken).ConfigureAwait(false);

                var list = images.ToList();
                int minWidth;

                foreach (var imageType in _singularImages)
                {
                    if (!IsEnabled(savedOptions, imageType, item))
                    {
                        continue;
                    }

                    if (!HasImage(item, imageType) || (refreshOptions.IsReplacingImage(imageType) && !downloadedImages.Contains(imageType)))
                    {
                        minWidth = savedOptions.GetMinWidth(imageType);
                        var downloaded = await DownloadImage(item, libraryOptions, provider, result, list, minWidth, imageType, cancellationToken).ConfigureAwait(false);

                        if (downloaded)
                        {
                            downloadedImages.Add(imageType);
                        }
                    }
                }

                minWidth = savedOptions.GetMinWidth(ImageType.Backdrop);
                await DownloadBackdrops(item, libraryOptions, ImageType.Backdrop, backdropLimit, provider, result, list, minWidth, cancellationToken).ConfigureAwait(false);

                var hasScreenshots = item as IHasScreenshots;
                if (hasScreenshots != null)
                {
                    minWidth = savedOptions.GetMinWidth(ImageType.Screenshot);
                    await DownloadBackdrops(item, libraryOptions, ImageType.Screenshot, screenshotLimit, provider, result, list, minWidth, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error in {provider}", provider.Name);
            }
        }

        private bool IsEnabled(TypeOptions options, ImageType type, BaseItem item)
        {
            return options.IsEnabled(type);
        }

        private void ClearImages(BaseItem item, ImageType type)
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

                try
                {
                    _fileSystem.DeleteFile(image.Path);
                    deleted = true;
                }
                catch (FileNotFoundException)
                {

                }
            }

            item.RemoveImages(deletedImages);

            if (deleted)
            {
                item.ValidateImages(new DirectoryService(_fileSystem));
            }
        }

        public bool MergeImages(BaseItem item, List<LocalImageInfo> images)
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

                        var newDateModified = _fileSystem.GetLastWriteTimeUtc(image.FileInfo);

                        // If date changed then we need to reset saved image dimensions
                        if (currentImage.DateModified != newDateModified && (currentImage.Width > 0 || currentImage.Height > 0))
                        {
                            currentImage.Width = 0;
                            currentImage.Height = 0;
                            changed = true;
                        }

                        currentImage.DateModified = newDateModified;
                    }
                }
                else
                {
                    var existing = item.GetImageInfo(type, 0);
                    if (existing != null)
                    {
                        if (existing.IsLocalFile && !File.Exists(existing.Path))
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

        private bool UpdateMultiImages(BaseItem item, List<LocalImageInfo> images, ImageType type)
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

        private async Task<bool> DownloadImage(BaseItem item, LibraryOptions libraryOptions,
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

            if (EnableImageStub(item, type, libraryOptions) && eligibleImages.Count > 0)
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

        private bool EnableImageStub(BaseItem item, ImageType type, LibraryOptions libraryOptions)
        {
            if (item is LiveTvProgram)
            {
                return true;
            }

            if (!item.IsFileProtocol)
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

            if (libraryOptions.DownloadImagesInAdvance)
            {
                return false;
            }

            //if (!item.IsSaveLocalMetadataEnabled())
            //{
            //    return true;
            //}

            return true;
        }

        private void SaveImageStub(BaseItem item, ImageType imageType, IEnumerable<string> urls)
        {
            var newIndex = item.AllowsMultipleImages(imageType) ? item.GetImages(imageType).Count() : 0;

            SaveImageStub(item, imageType, urls, newIndex);
        }

        private void SaveImageStub(BaseItem item, ImageType imageType, IEnumerable<string> urls, int newIndex)
        {
            var path = string.Join("|", urls.Take(1).ToArray());

            item.SetImage(new ItemImageInfo
            {
                Path = path,
                Type = imageType

            }, newIndex);
        }

        private async Task DownloadBackdrops(BaseItem item, LibraryOptions libraryOptions, ImageType imageType, int limit, IRemoteImageProvider provider, RefreshResult result, IEnumerable<RemoteImageInfo> images, int minWidth, CancellationToken cancellationToken)
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

                if (EnableImageStub(item, imageType, libraryOptions))
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
                            if (item.GetImages(imageType).Any(i => _fileSystem.GetFileInfo(i.Path).Length == response.ContentLength.Value))
                            {
                                response.Content.Dispose();
                                continue;
                            }
                        }
                        catch (IOException ex)
                        {
                            _logger.LogError(ex, "Error examining images");
                        }
                    }

                    await _providerManager.SaveImage(item, response.Content, response.ContentType, imageType, null, cancellationToken).ConfigureAwait(false);
                    result.UpdateType = result.UpdateType | ItemUpdateType.ImageUpdate;
                }
                catch (HttpException ex)
                {
                    // Sometimes providers send back bad urls. Just move onto the next image
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
