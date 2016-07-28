using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.Manager
{
    /// <summary>
    /// Class ImageSaver
    /// </summary>
    public class ImageSaver
    {
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// The _config
        /// </summary>
        private readonly IServerConfigurationManager _config;

        /// <summary>
        /// The _directory watchers
        /// </summary>
        private readonly ILibraryMonitor _libraryMonitor;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSaver" /> class.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <param name="libraryMonitor">The directory watchers.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="logger">The logger.</param>
        public ImageSaver(IServerConfigurationManager config, ILibraryMonitor libraryMonitor, IFileSystem fileSystem, ILogger logger)
        {
            _config = config;
            _libraryMonitor = libraryMonitor;
            _fileSystem = fileSystem;
            _logger = logger;
        }

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
        /// <exception cref="System.ArgumentNullException">mimeType</exception>
        public Task SaveImage(IHasImages item, Stream source, string mimeType, ImageType type, int? imageIndex, CancellationToken cancellationToken)
        {
            return SaveImage(item, source, mimeType, type, imageIndex, null, cancellationToken);
        }

        public async Task SaveImage(IHasImages item, Stream source, string mimeType, ImageType type, int? imageIndex, string internalCacheKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(mimeType))
            {
                throw new ArgumentNullException("mimeType");
            }

            var saveLocally = item.SupportsLocalMetadata && item.IsSaveLocalMetadataEnabled() && !item.IsOwnedItem && !(item is Audio);

            if (item is User)
            {
                saveLocally = true;
            }

            if (type != ImageType.Primary && item is Episode)
            {
                saveLocally = false;
            }

            var locationType = item.LocationType;
            if (locationType == LocationType.Remote || locationType == LocationType.Virtual)
            {
                saveLocally = false;

                var season = item as Season;

                // If season is virtual under a physical series, save locally if using compatible convention
                if (season != null && _config.Configuration.ImageSavingConvention == ImageSavingConvention.Compatible)
                {
                    var series = season.Series;

                    if (series != null && series.SupportsLocalMetadata && series.IsSaveLocalMetadataEnabled())
                    {
                        saveLocally = true;
                    }
                }
            }
            if (!string.IsNullOrEmpty(internalCacheKey))
            {
                saveLocally = false;
            }

            if (!imageIndex.HasValue && item.AllowsMultipleImages(type))
            {
                imageIndex = item.GetImages(type).Count();
            }

            var index = imageIndex ?? 0;

            var paths = GetSavePaths(item, type, imageIndex, mimeType, saveLocally);

            var retryPaths = GetSavePaths(item, type, imageIndex, mimeType, false);

            // If there are more than one output paths, the stream will need to be seekable
            var memoryStream = new MemoryStream();
            using (source)
            {
                await source.CopyToAsync(memoryStream).ConfigureAwait(false);
            }

            source = memoryStream;

            var currentImage = GetCurrentImage(item, type, index);
            var currentImageIsLocalFile = currentImage != null && currentImage.IsLocalFile;
            var currentImagePath = currentImage == null ? null : currentImage.Path;

            var savedPaths = new List<string>();

            using (source)
            {
                var currentPathIndex = 0;

                foreach (var path in paths)
                {
                    source.Position = 0;
                    string retryPath = null;
                    if (paths.Length == retryPaths.Length)
                    {
                        retryPath = retryPaths[currentPathIndex];
                    }
                    var savedPath = await SaveImageToLocation(source, path, retryPath, cancellationToken).ConfigureAwait(false);
                    savedPaths.Add(savedPath);
                    currentPathIndex++;
                }
            }

            // Set the path into the item
            SetImagePath(item, type, imageIndex, savedPaths[0]);

            // Delete the current path
            if (currentImageIsLocalFile && !savedPaths.Contains(currentImagePath, StringComparer.OrdinalIgnoreCase))
            {
                var currentPath = currentImagePath;

                _logger.Debug("Deleting previous image {0}", currentPath);

                _libraryMonitor.ReportFileSystemChangeBeginning(currentPath);

                try
                {
                    var currentFile = new FileInfo(currentPath);

                    // This will fail if the file is hidden
                    if (currentFile.Exists)
                    {
                        if ((currentFile.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                        {
                            currentFile.Attributes &= ~FileAttributes.Hidden;
                        }

                        _fileSystem.DeleteFile(currentFile.FullName);
                    }
                }
                finally
                {
                    _libraryMonitor.ReportFileSystemChangeComplete(currentPath, false);
                }
            }
        }

        private async Task<string> SaveImageToLocation(Stream source, string path, string retryPath, CancellationToken cancellationToken)
        {
            try
            {
                await SaveImageToLocation(source, path, cancellationToken).ConfigureAwait(false);
                return path;
            }
            catch (UnauthorizedAccessException)
            {
                var retry = !string.IsNullOrWhiteSpace(retryPath) &&
                    !string.Equals(path, retryPath, StringComparison.OrdinalIgnoreCase);

                if (retry)
                {
                    _logger.Error("UnauthorizedAccessException - Access to path {0} is denied. Will retry saving to {1}", path, retryPath);
                }
                else
                {
                    throw;
                }
            }

            source.Position = 0;
            await SaveImageToLocation(source, retryPath, cancellationToken).ConfigureAwait(false);
            return retryPath;
        }

        /// <summary>
        /// Saves the image to location.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task SaveImageToLocation(Stream source, string path, CancellationToken cancellationToken)
        {
            _logger.Debug("Saving image to {0}", path);

            var parentFolder = Path.GetDirectoryName(path);

            _libraryMonitor.ReportFileSystemChangeBeginning(path);
            _libraryMonitor.ReportFileSystemChangeBeginning(parentFolder);

            try
            {
                _fileSystem.CreateDirectory(Path.GetDirectoryName(path));

                // If the file is currently hidden we'll have to remove that or the save will fail
                var file = new FileInfo(path);

                // This will fail if the file is hidden
                if (file.Exists)
                {
                    if ((file.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                    {
                        file.Attributes &= ~FileAttributes.Hidden;
                    }
                }

                using (var fs = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await source.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, cancellationToken)
                            .ConfigureAwait(false);
                }

                if (_config.Configuration.SaveMetadataHidden)
                {
                    file.Refresh();

                    // Add back the attribute
                    file.Attributes |= FileAttributes.Hidden;
                }
            }
            finally
            {
                _libraryMonitor.ReportFileSystemChangeComplete(path, false);
                _libraryMonitor.ReportFileSystemChangeComplete(parentFolder, false);
            }
        }

        /// <summary>
        /// Gets the save paths.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <param name="saveLocally">if set to <c>true</c> [save locally].</param>
        /// <returns>IEnumerable{System.String}.</returns>
        private string[] GetSavePaths(IHasImages item, ImageType type, int? imageIndex, string mimeType, bool saveLocally)
        {
            if (!saveLocally || (_config.Configuration.ImageSavingConvention == ImageSavingConvention.Legacy))
            {
                return new[] { GetStandardSavePath(item, type, imageIndex, mimeType, saveLocally) };
            }

            return GetCompatibleSavePaths(item, type, imageIndex, mimeType);
        }

        /// <summary>
        /// Gets the current image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// imageIndex
        /// or
        /// imageIndex
        /// </exception>
        private ItemImageInfo GetCurrentImage(IHasImages item, ImageType type, int imageIndex)
        {
            return item.GetImageInfo(type, imageIndex);
        }

        /// <summary>
        /// Sets the image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="path">The path.</param>
        /// <exception cref="System.ArgumentNullException">imageIndex
        /// or
        /// imageIndex</exception>
        private void SetImagePath(IHasImages item, ImageType type, int? imageIndex, string path)
        {
            item.SetImagePath(type, imageIndex ?? 0, _fileSystem.GetFileInfo(path));
        }

        /// <summary>
        /// Gets the save path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <param name="saveLocally">if set to <c>true</c> [save locally].</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// imageIndex
        /// or
        /// imageIndex
        /// </exception>
        private string GetStandardSavePath(IHasImages item, ImageType type, int? imageIndex, string mimeType, bool saveLocally)
        {
            var season = item as Season;
            var extension = MimeTypes.ToExtension(mimeType);

            if (type == ImageType.Thumb && saveLocally)
            {
                if (season != null && season.IndexNumber.HasValue)
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                           ? "-specials"
                                           : season.IndexNumber.Value.ToString("00", UsCulture);

                    var imageFilename = "season" + seasonMarker + "-landscape" + extension;

                    return Path.Combine(seriesFolder, imageFilename);
                }

                if (item.IsInMixedFolder)
                {
                    return GetSavePathForItemInMixedFolder(item, type, "landscape", extension);
                }

                return Path.Combine(item.ContainingFolderPath, "landscape" + extension);
            }

            if (type == ImageType.Banner && saveLocally)
            {
                if (season != null && season.IndexNumber.HasValue)
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                           ? "-specials"
                                           : season.IndexNumber.Value.ToString("00", UsCulture);

                    var imageFilename = "season" + seasonMarker + "-banner" + extension;

                    return Path.Combine(seriesFolder, imageFilename);
                }
            }

            string filename;
            var folderName = item is MusicAlbum ||
                item is MusicArtist ||
                item is PhotoAlbum ||
                (saveLocally && _config.Configuration.ImageSavingConvention == ImageSavingConvention.Legacy) ?
                "folder" :
                "poster";

            switch (type)
            {
                case ImageType.Art:
                    filename = "clearart";
                    break;
                case ImageType.BoxRear:
                    filename = "back";
                    break;
                case ImageType.Thumb:
                    filename = "landscape";
                    break;
                case ImageType.Disc:
                    filename = item is MusicAlbum ? "cdart" : "disc";
                    break;
                case ImageType.Primary:
                    filename = item is Episode ? _fileSystem.GetFileNameWithoutExtension(item.Path) : folderName;
                    break;
                case ImageType.Backdrop:
                    filename = GetBackdropSaveFilename(item.GetImages(type), "backdrop", "backdrop", imageIndex);
                    break;
                case ImageType.Screenshot:
                    filename = GetBackdropSaveFilename(item.GetImages(type), "screenshot", "screenshot", imageIndex);
                    break;
                default:
                    filename = type.ToString().ToLower();
                    break;
            }

            if (string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                extension = ".jpg";
            }

            extension = extension.ToLower();

            string path = null;

            if (saveLocally)
            {
                if (type == ImageType.Primary && item is Episode)
                {
                    path = Path.Combine(Path.GetDirectoryName(item.Path), "metadata", filename + extension);
                }

                else if (item.IsInMixedFolder)
                {
                    path = GetSavePathForItemInMixedFolder(item, type, filename, extension);
                }

                if (string.IsNullOrEmpty(path))
                {
                    path = Path.Combine(item.ContainingFolderPath, filename + extension);
                }
            }

            // None of the save local conditions passed, so store it in our internal folders
            if (string.IsNullOrEmpty(path))
            {
                if (string.IsNullOrEmpty(filename))
                {
                    filename = folderName;
                }
                path = Path.Combine(item.GetInternalMetadataPath(), filename + extension);
            }

            return path;
        }

        private string GetBackdropSaveFilename(IEnumerable<ItemImageInfo> images, string zeroIndexFilename, string numberedIndexPrefix, int? index)
        {
            if (index.HasValue && index.Value == 0)
            {
                return zeroIndexFilename;
            }

            var filenames = images.Select(i => _fileSystem.GetFileNameWithoutExtension(i.Path)).ToList();

            var current = 1;
            while (filenames.Contains(numberedIndexPrefix + current.ToString(UsCulture), StringComparer.OrdinalIgnoreCase))
            {
                current++;
            }

            return numberedIndexPrefix + current.ToString(UsCulture);
        }

        /// <summary>
        /// Gets the compatible save paths.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        /// <exception cref="System.ArgumentNullException">imageIndex</exception>
        private string[] GetCompatibleSavePaths(IHasImages item, ImageType type, int? imageIndex, string mimeType)
        {
            var season = item as Season;

            var extension = MimeTypes.ToExtension(mimeType);

            // Backdrop paths
            if (type == ImageType.Backdrop)
            {
                if (!imageIndex.HasValue)
                {
                    throw new ArgumentNullException("imageIndex");
                }

                if (imageIndex.Value == 0)
                {
                    if (item.IsInMixedFolder)
                    {
                        return new[] { GetSavePathForItemInMixedFolder(item, type, "fanart", extension) };
                    }

                    if (season != null && season.IndexNumber.HasValue)
                    {
                        var seriesFolder = season.SeriesPath;

                        var seasonMarker = season.IndexNumber.Value == 0
                                               ? "-specials"
                                               : season.IndexNumber.Value.ToString("00", UsCulture);

                        var imageFilename = "season" + seasonMarker + "-fanart" + extension;

                        return new[] { Path.Combine(seriesFolder, imageFilename) };
                    }

                    return new[]
                        {
                            Path.Combine(item.ContainingFolderPath, "fanart" + extension)
                        };
                }

                var outputIndex = imageIndex.Value;

                if (item.IsInMixedFolder)
                {
                    return new[] { GetSavePathForItemInMixedFolder(item, type, "fanart" + outputIndex.ToString(UsCulture), extension) };
                }

                var extraFanartFilename = GetBackdropSaveFilename(item.GetImages(ImageType.Backdrop), "fanart", "fanart", outputIndex);

                var list = new List<string>
                {
                    Path.Combine(item.ContainingFolderPath, "extrafanart", extraFanartFilename + extension)
                };

                if (EnableExtraThumbsDuplication)
                {
                    list.Add(Path.Combine(item.ContainingFolderPath, "extrathumbs", "thumb" + outputIndex.ToString(UsCulture) + extension));
                }
                return list.ToArray();
            }

            if (type == ImageType.Primary)
            {
                if (season != null && season.IndexNumber.HasValue)
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                           ? "-specials"
                                           : season.IndexNumber.Value.ToString("00", UsCulture);

                    var imageFilename = "season" + seasonMarker + "-poster" + extension;

                    return new[] { Path.Combine(seriesFolder, imageFilename) };
                }

                if (item is Episode)
                {
                    var seasonFolder = Path.GetDirectoryName(item.Path);

                    var imageFilename = _fileSystem.GetFileNameWithoutExtension(item.Path) + "-thumb" + extension;

                    return new[] { Path.Combine(seasonFolder, imageFilename) };
                }

                if (item.IsInMixedFolder || item is MusicVideo)
                {
                    return new[] { GetSavePathForItemInMixedFolder(item, type, string.Empty, extension) };
                }

                if (item is MusicAlbum || item is MusicArtist)
                {
                    return new[] { Path.Combine(item.ContainingFolderPath, "folder" + extension) };
                }

                return new[] { Path.Combine(item.ContainingFolderPath, "poster" + extension) };
            }

            // All other paths are the same
            return new[] { GetStandardSavePath(item, type, imageIndex, mimeType, true) };
        }

        private bool EnableExtraThumbsDuplication
        {
            get
            {
                var config = _config.GetConfiguration<XbmcMetadataOptions>("xbmcmetadata");

                return config.EnableExtraThumbsDuplication;
            }
        }

        /// <summary>
        /// Gets the save path for item in mixed folder.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="extension">The extension.</param>
        /// <returns>System.String.</returns>
        private string GetSavePathForItemInMixedFolder(IHasImages item, ImageType type, string imageFilename, string extension)
        {
            if (type == ImageType.Primary)
            {
                imageFilename = "poster";
            }
            var folder = Path.GetDirectoryName(item.Path);

            return Path.Combine(folder, _fileSystem.GetFileNameWithoutExtension(item.Path) + "-" + imageFilename + extension);
        }
    }
}
