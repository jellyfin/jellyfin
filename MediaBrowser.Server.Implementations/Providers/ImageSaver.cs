using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Providers
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
        /// The remote image cache
        /// </summary>
        private readonly FileSystemRepository _remoteImageCache;
        /// <summary>
        /// The _directory watchers
        /// </summary>
        private readonly IDirectoryWatchers _directoryWatchers;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSaver"/> class.
        /// </summary>
        /// <param name="config">The config.</param>
        /// <param name="directoryWatchers">The directory watchers.</param>
        public ImageSaver(IServerConfigurationManager config, IDirectoryWatchers directoryWatchers)
        {
            _config = config;
            _directoryWatchers = directoryWatchers;
            _remoteImageCache = new FileSystemRepository(config.ApplicationPaths.DownloadedImagesDataPath);
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
        public async Task SaveImage(BaseItem item, Stream source, string mimeType, ImageType type, int? imageIndex, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(mimeType))
            {
                throw new ArgumentNullException("mimeType");
            }

            var saveLocally = _config.Configuration.SaveLocalMeta;

            if (item is IItemByName)
            {
                saveLocally = true;
            }
            else if (item is User)
            {
                saveLocally = true;
            }
            else if (item is Audio || item.Parent == null || string.IsNullOrEmpty(item.MetaLocation))
            {
                saveLocally = false;
            }

            if (type != ImageType.Primary)
            {
                if (item is Episode)
                {
                    saveLocally = false;
                }
            }

            if (item.LocationType == LocationType.Remote || item.LocationType == LocationType.Virtual)
            {
                saveLocally = false;
            }

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

            var currentPath = GetCurrentImagePath(item, type, imageIndex);

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

            // Set the path into the BaseItem
            SetImagePath(item, type, imageIndex, paths[0]);

            // Delete the current path
            if (!string.IsNullOrEmpty(currentPath) && !paths.Contains(currentPath, StringComparer.OrdinalIgnoreCase))
            {
                _directoryWatchers.TemporarilyIgnore(currentPath);

                try
                {
                    File.Delete(currentPath);
                }
                finally
                {
                    _directoryWatchers.RemoveTempIgnore(currentPath);
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
            _directoryWatchers.TemporarilyIgnore(path);

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

                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                {
                    await source.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _directoryWatchers.RemoveTempIgnore(path);
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
        private string GetCurrentImagePath(BaseItem item, ImageType type, int? imageIndex)
        {
            switch (type)
            {
                case ImageType.Screenshot:

                    if (!imageIndex.HasValue)
                    {
                        throw new ArgumentNullException("imageIndex");
                    }
                    return item.ScreenshotImagePaths.Count > imageIndex.Value ? item.ScreenshotImagePaths[imageIndex.Value] : null;
                case ImageType.Backdrop:
                    if (!imageIndex.HasValue)
                    {
                        throw new ArgumentNullException("imageIndex");
                    }
                    return item.BackdropImagePaths.Count > imageIndex.Value ? item.BackdropImagePaths[imageIndex.Value] : null;
                default:
                    return item.GetImage(type);
            }
        }

        /// <summary>
        /// Sets the image path.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <param name="path">The path.</param>
        /// <exception cref="System.ArgumentNullException">
        /// imageIndex
        /// or
        /// imageIndex
        /// </exception>
        private void SetImagePath(BaseItem item, ImageType type, int? imageIndex, string path)
        {
            switch (type)
            {
                case ImageType.Screenshot:

                    if (!imageIndex.HasValue)
                    {
                        throw new ArgumentNullException("imageIndex");
                    }

                    if (item.ScreenshotImagePaths.Count > imageIndex.Value)
                    {
                        item.ScreenshotImagePaths[imageIndex.Value] = path;
                    }
                    else
                    {
                        item.ScreenshotImagePaths.Add(path);
                    }
                    break;
                case ImageType.Backdrop:
                    if (!imageIndex.HasValue)
                    {
                        throw new ArgumentNullException("imageIndex");
                    }
                    if (item.BackdropImagePaths.Count > imageIndex.Value)
                    {
                        item.BackdropImagePaths[imageIndex.Value] = path;
                    }
                    else
                    {
                        item.BackdropImagePaths.Add(path);
                    }
                    break;
                default:
                    item.SetImage(type, path);
                    break;
            }
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
        private string GetStandardSavePath(BaseItem item, ImageType type, int? imageIndex, string mimeType, bool saveLocally)
        {
            string filename;

            switch (type)
            {
                case ImageType.Art:
                    filename = "clearart";
                    break;
                case ImageType.Primary:
                    filename = item is Episode ? Path.GetFileNameWithoutExtension(item.Path) : "folder";
                    break;
                case ImageType.Backdrop:
                    if (!imageIndex.HasValue)
                    {
                        throw new ArgumentNullException("imageIndex");
                    }
                    filename = imageIndex.Value == 0 ? "backdrop" : "backdrop" + imageIndex.Value.ToString(UsCulture);
                    break;
                case ImageType.Screenshot:
                    if (!imageIndex.HasValue)
                    {
                        throw new ArgumentNullException("imageIndex");
                    }
                    filename = imageIndex.Value == 0 ? "screenshot" : "screenshot" + imageIndex.Value.ToString(UsCulture);
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

            string path = null;

            if (saveLocally)
            {
                if (item.IsInMixedFolder && !(item is Episode))
                {
                    path = GetSavePathForItemInMixedFolder(item, type, filename, extension);
                }

                if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(item.MetaLocation))
                {
                    path = Path.Combine(item.MetaLocation, filename + extension.ToLower());
                }
            }

            filename += "." + extension.ToLower();

            // None of the save local conditions passed, so store it in our internal folders
            if (string.IsNullOrEmpty(path))
            {
                path = _remoteImageCache.GetResourcePath(item.GetType().FullName + item.Id, filename);
            }

            return path;
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
        private string[] GetCompatibleSavePaths(BaseItem item, ImageType type, int? imageIndex, string mimeType)
        {
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
                    if (item is Season && item.IndexNumber.HasValue)
                    {
                        var seriesFolder = Path.GetDirectoryName(item.Path);

                        var seasonMarker = item.IndexNumber.Value == 0
                                               ? "-specials"
                                               : item.IndexNumber.Value.ToString("00", UsCulture);

                        var imageFilename = "season" + seasonMarker + "-fanart" + extension;

                        return new[] { Path.Combine(seriesFolder, imageFilename) };
                    }

                    return new[]
                        {
                            Path.Combine(item.MetaLocation, "fanart" + extension)
                        };
                }

                var outputIndex = imageIndex.Value;

                return new[]
                    {
                        Path.Combine(item.MetaLocation, "extrafanart", "fanart" + outputIndex.ToString(UsCulture) + extension),
                        Path.Combine(item.MetaLocation, "extrathumbs", "thumb" + outputIndex.ToString(UsCulture) + extension)
                    };
            }

            if (type == ImageType.Primary)
            {
                if (item is Season && item.IndexNumber.HasValue)
                {
                    var seriesFolder = Path.GetDirectoryName(item.Path);

                    var seasonMarker = item.IndexNumber.Value == 0
                                           ? "-specials"
                                           : item.IndexNumber.Value.ToString("00", UsCulture);

                    var imageFilename = "season" + seasonMarker + "-poster" + extension;

                    return new[] { Path.Combine(seriesFolder, imageFilename) };
                }

                if (item is Episode)
                {
                    var seasonFolder = Path.GetDirectoryName(item.Path);

                    var imageFilename = Path.GetFileNameWithoutExtension(item.Path) + "-thumb" + extension;

                    return new[] { Path.Combine(seasonFolder, imageFilename) };
                }

                if (item.IsInMixedFolder)
                {
                    return new[] { GetSavePathForItemInMixedFolder(item, type, string.Empty, extension) };
                }

                var filename = "poster" + extension;
                return new[] { Path.Combine(item.MetaLocation, filename) };
            }

            if (type == ImageType.Banner)
            {
                if (item is Season && item.IndexNumber.HasValue)
                {
                    var seriesFolder = Path.GetDirectoryName(item.Path);

                    var seasonMarker = item.IndexNumber.Value == 0
                                           ? "-specials"
                                           : item.IndexNumber.Value.ToString("00", UsCulture);

                    var imageFilename = "season" + seasonMarker + "-banner" + extension;

                    return new[] { Path.Combine(seriesFolder, imageFilename) };
                }
            }

            if (type == ImageType.Thumb)
            {
                if (item is Season && item.IndexNumber.HasValue)
                {
                    var seriesFolder = Path.GetDirectoryName(item.Path);

                    var seasonMarker = item.IndexNumber.Value == 0
                                           ? "-specials"
                                           : item.IndexNumber.Value.ToString("00", UsCulture);

                    var imageFilename = "season" + seasonMarker + "-landscape" + extension;

                    return new[] { Path.Combine(seriesFolder, imageFilename) };
                }
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
