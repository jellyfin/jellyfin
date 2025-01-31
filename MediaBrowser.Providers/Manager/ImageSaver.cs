#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using Microsoft.Extensions.Logging;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using MusicAlbum = MediaBrowser.Controller.Entities.Audio.MusicAlbum;
using Person = MediaBrowser.Controller.Entities.Person;
using Season = MediaBrowser.Controller.Entities.TV.Season;

namespace MediaBrowser.Providers.Manager
{
    /// <summary>
    /// Class ImageSaver.
    /// </summary>
    public class ImageSaver
    {
        /// <summary>
        /// The _config.
        /// </summary>
        private readonly IServerConfigurationManager _config;

        /// <summary>
        /// The _directory watchers.
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

        private bool EnableExtraThumbsDuplication
        {
            get
            {
                var config = _config.GetConfiguration<XbmcMetadataOptions>("xbmcmetadata");

                return config.EnableExtraThumbsDuplication;
            }
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
        /// <exception cref="ArgumentNullException">mimeType.</exception>
        public Task SaveImage(BaseItem item, Stream source, string mimeType, ImageType type, int? imageIndex, CancellationToken cancellationToken)
        {
            return SaveImage(item, source, mimeType, type, imageIndex, null, cancellationToken);
        }

        public async Task SaveImage(BaseItem item, Stream source, string mimeType, ImageType type, int? imageIndex, bool? saveLocallyWithMedia, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(mimeType);

            var saveLocally = item.SupportsLocalMetadata && item.IsSaveLocalMetadataEnabled() && !item.ExtraType.HasValue && item is not Audio;

            if (type != ImageType.Primary && item is Episode)
            {
                saveLocally = false;
            }

            if (!item.IsFileProtocol)
            {
                saveLocally = false;

                // If season is virtual under a physical series, save locally
                if (item is Season season)
                {
                    var series = season.Series;

                    if (series is not null && series.SupportsLocalMetadata && series.IsSaveLocalMetadataEnabled())
                    {
                        saveLocally = true;
                    }
                }
            }

            if (saveLocallyWithMedia.HasValue && !saveLocallyWithMedia.Value)
            {
                saveLocally = saveLocallyWithMedia.Value;
            }

            if (!imageIndex.HasValue && item.AllowsMultipleImages(type))
            {
                imageIndex = item.GetImages(type).Count();
            }

            var index = imageIndex ?? 0;

            var paths = GetSavePaths(item, type, imageIndex, mimeType, saveLocally);

            string[] retryPaths = [];
            if (saveLocally)
            {
                retryPaths = GetSavePaths(item, type, imageIndex, mimeType, false);
            }

            // If there are more than one output paths, the stream will need to be seekable
            if (paths.Length > 1 && !source.CanSeek)
            {
                var memoryStream = new MemoryStream();
                await using (source.ConfigureAwait(false))
                {
                    await source.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                }

                source = memoryStream;
            }

            var currentImage = GetCurrentImage(item, type, index);
            var currentImageIsLocalFile = currentImage is not null && currentImage.IsLocalFile;
            var currentImagePath = currentImage?.Path;

            var savedPaths = new List<string>();

            await using (source.ConfigureAwait(false))
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    if (i != 0)
                    {
                        source.Position = 0;
                    }

                    string retryPath = null;
                    if (paths.Length == retryPaths.Length)
                    {
                        retryPath = retryPaths[i];
                    }

                    var savedPath = await SaveImageToLocation(source, paths[i], retryPath, cancellationToken).ConfigureAwait(false);
                    savedPaths.Add(savedPath);
                }
            }

            // Set the path into the item
            SetImagePath(item, type, imageIndex, savedPaths[0]);

            // Delete the current path
            if (currentImageIsLocalFile
                && !savedPaths.Contains(currentImagePath, StringComparison.OrdinalIgnoreCase)
                && (saveLocally || currentImagePath.Contains(_config.ApplicationPaths.InternalMetadataPath, StringComparison.OrdinalIgnoreCase)))
            {
                var currentPath = currentImagePath;

                _logger.LogInformation("Deleting previous image {0}", currentPath);

                _libraryMonitor.ReportFileSystemChangeBeginning(currentPath);

                try
                {
                    _fileSystem.DeleteFile(currentPath);

                    // Remove local episode metadata directory if it exists and is empty
                    var directory = Path.GetDirectoryName(currentPath);
                    if (item is Episode && directory.Equals("metadata", StringComparison.Ordinal))
                    {
                        var parentDirectoryPath = Directory.GetParent(currentPath).FullName;
                        if (_fileSystem.DirectoryExists(parentDirectoryPath) && !_fileSystem.GetFiles(parentDirectoryPath).Any())
                        {
                            try
                            {
                                _logger.LogInformation("Deleting empty local metadata folder {Folder}", parentDirectoryPath);
                                Directory.Delete(parentDirectoryPath);
                            }
                            catch (UnauthorizedAccessException ex)
                            {
                                _logger.LogError(ex, "Error deleting directory {Path}", parentDirectoryPath);
                            }
                            catch (IOException ex)
                            {
                                _logger.LogError(ex, "Error deleting directory {Path}", parentDirectoryPath);
                            }
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                }
                finally
                {
                    _libraryMonitor.ReportFileSystemChangeComplete(currentPath, false);
                }
            }
        }

        public async Task SaveImage(Stream source, string path)
        {
            await SaveImageToLocation(source, path, path, CancellationToken.None).ConfigureAwait(false);
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
                    _logger.LogError("UnauthorizedAccessException - Access to path {0} is denied. Will retry saving to {1}", path, retryPath);
                }
                else
                {
                    throw;
                }
            }
            catch (IOException ex)
            {
                var retry = !string.IsNullOrWhiteSpace(retryPath) &&
                    !string.Equals(path, retryPath, StringComparison.OrdinalIgnoreCase);

                if (retry)
                {
                    _logger.LogError(ex, "IOException saving to {0}. Will retry saving to {1}", path, retryPath);
                }
                else
                {
                    throw;
                }
            }

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
            _logger.LogDebug("Saving image to {0}", path);

            var parentFolder = Path.GetDirectoryName(path);

            try
            {
                _libraryMonitor.ReportFileSystemChangeBeginning(path);
                _libraryMonitor.ReportFileSystemChangeBeginning(parentFolder);

                Directory.CreateDirectory(Path.GetDirectoryName(path));

                _fileSystem.SetAttributes(path, false, false);

                var fileStreamOptions = AsyncFile.WriteOptions;
                fileStreamOptions.Mode = FileMode.Create;
                if (source.CanSeek)
                {
                    fileStreamOptions.PreallocationSize = source.Length;
                }

                var fs = new FileStream(path, fileStreamOptions);
                await using (fs.ConfigureAwait(false))
                {
                    await source.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                }

                if (_config.Configuration.SaveMetadataHidden)
                {
                    SetHidden(path, true);
                }
            }
            finally
            {
                _libraryMonitor.ReportFileSystemChangeComplete(path, false);
                _libraryMonitor.ReportFileSystemChangeComplete(parentFolder, false);
            }
        }

        private void SetHidden(string path, bool hidden)
        {
            try
            {
                _fileSystem.SetHidden(path, hidden);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting hidden attribute on {0}", path);
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
        private string[] GetSavePaths(BaseItem item, ImageType type, int? imageIndex, string mimeType, bool saveLocally)
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
        /// <exception cref="ArgumentNullException">
        /// imageIndex
        /// or
        /// imageIndex.
        /// </exception>
        private ItemImageInfo GetCurrentImage(BaseItem item, ImageType type, int imageIndex)
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
        /// <exception cref="ArgumentNullException">imageIndex
        /// or
        /// imageIndex.
        /// </exception>
        private void SetImagePath(BaseItem item, ImageType type, int? imageIndex, string path)
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
        /// <exception cref="ArgumentNullException">
        /// imageIndex
        /// or
        /// imageIndex.
        /// </exception>
        private string GetStandardSavePath(BaseItem item, ImageType type, int? imageIndex, string mimeType, bool saveLocally)
        {
            var season = item as Season;
            var extension = MimeTypes.ToExtension(mimeType);

            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Unable to determine image file extension from mime type {0}", mimeType));
            }

            if (string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                extension = ".jpg";
            }

            extension = extension.ToLowerInvariant();

            if (type == ImageType.Primary && saveLocally)
            {
                if (season is not null && season.IndexNumber.HasValue)
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                           ? "-specials"
                                           : season.IndexNumber.Value.ToString("00", CultureInfo.InvariantCulture);

                    var imageFilename = "season" + seasonMarker + "-poster" + extension;

                    return Path.Combine(seriesFolder, imageFilename);
                }
            }

            if (type == ImageType.Backdrop && saveLocally)
            {
                if (season is not null
                    && season.IndexNumber.HasValue
                    && (imageIndex is null || imageIndex == 0))
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                        ? "-specials"
                                        : season.IndexNumber.Value.ToString("00", CultureInfo.InvariantCulture);

                    var imageFilename = "season" + seasonMarker + "-fanart" + extension;

                    return Path.Combine(seriesFolder, imageFilename);
                }
            }

            if (type == ImageType.Thumb && saveLocally)
            {
                if (season is not null && season.IndexNumber.HasValue)
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                           ? "-specials"
                                           : season.IndexNumber.Value.ToString("00", CultureInfo.InvariantCulture);

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
                if (season is not null && season.IndexNumber.HasValue)
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                           ? "-specials"
                                           : season.IndexNumber.Value.ToString("00", CultureInfo.InvariantCulture);

                    var imageFilename = "season" + seasonMarker + "-banner" + extension;

                    return Path.Combine(seriesFolder, imageFilename);
                }
            }

            string filename;
            var folderName = item is MusicAlbum ||
                item is MusicArtist ||
                item is PhotoAlbum ||
                item is Person ||
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
                    filename = saveLocally && item is Episode ? Path.GetFileNameWithoutExtension(item.Path) : folderName;
                    break;
                case ImageType.Backdrop:
                    filename = GetBackdropSaveFilename(item.GetImages(type), "backdrop", "backdrop", imageIndex);
                    break;
                default:
                    filename = type.ToString().ToLowerInvariant();
                    break;
            }

            string path = null;
            if (saveLocally)
            {
                if (type == ImageType.Primary && item is Episode)
                {
                    path = Path.Combine(Path.GetDirectoryName(item.Path), filename + "-thumb" + extension);
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

            var filenames = images.Select(i => Path.GetFileNameWithoutExtension(i.Path)).ToList();

            var current = 1;
            while (filenames.Contains(numberedIndexPrefix + current.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
            {
                current++;
            }

            return numberedIndexPrefix + current.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gets the compatible save paths.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        /// <exception cref="ArgumentNullException">imageIndex.</exception>
        private string[] GetCompatibleSavePaths(BaseItem item, ImageType type, int? imageIndex, string mimeType)
        {
            var season = item as Season;

            var extension = MimeTypes.ToExtension(mimeType);

            // Backdrop paths
            if (type == ImageType.Backdrop)
            {
                if (!imageIndex.HasValue)
                {
                    throw new ArgumentNullException(nameof(imageIndex));
                }

                if (imageIndex.Value == 0)
                {
                    if (item.IsInMixedFolder)
                    {
                        return new[] { GetSavePathForItemInMixedFolder(item, type, "fanart", extension) };
                    }

                    if (season is not null && season.IndexNumber.HasValue)
                    {
                        var seriesFolder = season.SeriesPath;

                        var seasonMarker = season.IndexNumber.Value == 0
                                               ? "-specials"
                                               : season.IndexNumber.Value.ToString("00", CultureInfo.InvariantCulture);

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
                    return new[] { GetSavePathForItemInMixedFolder(item, type, "fanart" + outputIndex.ToString(CultureInfo.InvariantCulture), extension) };
                }

                var extraFanartFilename = GetBackdropSaveFilename(item.GetImages(ImageType.Backdrop), "fanart", "fanart", outputIndex);

                var list = new List<string>
                {
                    Path.Combine(item.ContainingFolderPath, "extrafanart", extraFanartFilename + extension)
                };

                if (EnableExtraThumbsDuplication)
                {
                    list.Add(Path.Combine(item.ContainingFolderPath, "extrathumbs", "thumb" + outputIndex.ToString(CultureInfo.InvariantCulture) + extension));
                }

                return list.ToArray();
            }

            if (type == ImageType.Primary)
            {
                if (season is not null && season.IndexNumber.HasValue)
                {
                    var seriesFolder = season.SeriesPath;

                    var seasonMarker = season.IndexNumber.Value == 0
                                           ? "-specials"
                                           : season.IndexNumber.Value.ToString("00", CultureInfo.InvariantCulture);

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
        private string GetSavePathForItemInMixedFolder(BaseItem item, ImageType type, string imageFilename, string extension)
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
