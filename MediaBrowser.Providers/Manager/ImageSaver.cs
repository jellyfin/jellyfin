using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public async Task SaveImage(IHasImages item, Stream source, string mimeType, ImageType type, int? imageIndex, CancellationToken cancellationToken)
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
            if (item is IItemByName)
            {
                var hasDualAccess = item as IHasDualAccess;
                if (hasDualAccess == null || hasDualAccess.IsAccessedByName)
                {
                    saveLocally = true;
                }
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

            if (!imageIndex.HasValue && item.AllowsMultipleImages(type))
            {
                imageIndex = item.GetImages(type).Count();
            }

            var index = imageIndex ?? 0;

            var paths = GetSavePaths(item, type, imageIndex, mimeType, saveLocally);

            // If there are more than one output paths, the stream will need to be seekable
            if (paths.Length > 1 && !source.CanSeek)
            {
                var memoryStream = new MemoryStream();
                using (source)
                {
                    await source.CopyToAsync(memoryStream).ConfigureAwait(false);
                }
                memoryStream.Position = 0;
                source = memoryStream;
            }

            var currentPath = GetCurrentImagePath(item, type, index);

            using (source)
            {
                var isFirst = true;

                foreach (var path in paths)
                {
                    // Seek back to the beginning
                    if (!isFirst)
                    {
                        source.Position = 0;
                    }

                    await SaveImageToLocation(source, path, cancellationToken).ConfigureAwait(false);

                    isFirst = false;
                }
            }

            // Set the path into the item
            SetImagePath(item, type, imageIndex, paths[0]);

            // Delete the current path
            if (!string.IsNullOrEmpty(currentPath) && !paths.Contains(currentPath, StringComparer.OrdinalIgnoreCase))
            {
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

                        currentFile.Delete();
                    }
                }
                finally
                {
                    _libraryMonitor.ReportFileSystemChangeComplete(currentPath, false);
                }
            }
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
                Directory.CreateDirectory(Path.GetDirectoryName(path));

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
                    await source.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);
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
            if (_config.Configuration.ImageSavingConvention == ImageSavingConvention.Legacy || !saveLocally)
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
        private string GetCurrentImagePath(IHasImages item, ImageType type, int imageIndex)
        {
            return item.GetImagePath(type, imageIndex);
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
            item.SetImagePath(type, imageIndex ?? 0, new FileInfo(path));
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
            string filename;

            switch (type)
            {
                case ImageType.Art:
                    filename = "clearart";
                    break;
                case ImageType.BoxRear:
                    filename = "back";
                    break;
                case ImageType.Disc:
                    filename = item is MusicAlbum ? "cdart" : "disc";
                    break;
                case ImageType.Primary:
                    filename = item is Episode ? Path.GetFileNameWithoutExtension(item.Path) : "folder";
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

            var extension = mimeType.Split('/').Last();

            if (string.Equals(extension, "jpeg", StringComparison.OrdinalIgnoreCase))
            {
                extension = "jpg";
            }

            extension = "." + extension.ToLower();

            string path = null;

            if (saveLocally)
            {
                if (item is Episode)
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
                    filename = "folder";
                }
                path = Path.Combine(_config.ApplicationPaths.GetInternalMetadataPath(item.Id), filename + extension);
            }

            return path;
        }

        private string GetBackdropSaveFilename(IEnumerable<ItemImageInfo> images, string zeroIndexFilename, string numberedIndexPrefix, int? index)
        {
            if (index.HasValue && index.Value == 0)
            {
                return zeroIndexFilename;
            }

            var filenames = images.Select(i => Path.GetFileNameWithoutExtension(i.Path)).ToList();

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

            var extension = mimeType.Split('/').Last();

            if (string.Equals(extension, "jpeg", StringComparison.OrdinalIgnoreCase))
            {
                extension = "jpg";
            }
            extension = "." + extension.ToLower();

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

                return new[]
                    {
                        Path.Combine(item.ContainingFolderPath, "extrafanart", extraFanartFilename + extension),
                        Path.Combine(item.ContainingFolderPath, "extrathumbs", "thumb" + outputIndex.ToString(UsCulture) + extension)
                    };
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

                    var imageFilename = Path.GetFileNameWithoutExtension(item.Path) + "-thumb" + extension;

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

            if (type == ImageType.Banner)
            {
                if (season != null && season.IndexNumber.HasValue)
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                           ? "-specials"
                                           : season.IndexNumber.Value.ToString("00", UsCulture);

                    var imageFilename = "season" + seasonMarker + "-banner" + extension;

                    return new[] { Path.Combine(seriesFolder, imageFilename) };
                }
            }

            if (type == ImageType.Thumb)
            {
                if (season != null && season.IndexNumber.HasValue)
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                           ? "-specials"
                                           : season.IndexNumber.Value.ToString("00", UsCulture);

                    var imageFilename = "season" + seasonMarker + "-landscape" + extension;

                    return new[] { Path.Combine(seriesFolder, imageFilename) };
                }

                if (item.IsInMixedFolder)
                {
                    return new[] { GetSavePathForItemInMixedFolder(item, type, "landscape", extension) };
                }

                return new[] { Path.Combine(item.ContainingFolderPath, "landscape" + extension) };
            }

            // All other paths are the same
            return new[] { GetStandardSavePath(item, type, imageIndex, mimeType, true) };
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

            return Path.Combine(folder, Path.GetFileNameWithoutExtension(item.Path) + "-" + imageFilename + extension);
        }
    }
}
