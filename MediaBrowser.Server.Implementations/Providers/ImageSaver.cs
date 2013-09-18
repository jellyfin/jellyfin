using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
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

            var path = GetSavePath(item, type, imageIndex, mimeType, saveLocally);

            var currentPath = GetCurrentImagePath(item, type, imageIndex);

            try
            {
                _directoryWatchers.TemporarilyIgnore(path);

                using (source)
                {
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                    {
                        await source.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                    }
                }

                SetImagePath(item, type, imageIndex, path);

                if (!string.IsNullOrEmpty(currentPath) && !string.Equals(path, currentPath, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(currentPath);
                }
            }
            finally
            {
                _directoryWatchers.RemoveTempIgnore(path);
            }

        }

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
        private string GetSavePath(BaseItem item, ImageType type, int? imageIndex, string mimeType, bool saveLocally)
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

            filename += "." + extension.ToLower();

            string path = null;

            if (saveLocally)
            {
                if (item.IsInMixedFolder && !(item is Episode))
                {
                    path = GetSavePathForItemInMixedFolder(item, type, filename, extension);
                }

                if (string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(item.MetaLocation))
                {
                    path = Path.Combine(item.MetaLocation, filename);
                }
            }

            // None of the save local conditions passed, so store it in our internal folders
            if (string.IsNullOrEmpty(path))
            {
                path = _remoteImageCache.GetResourcePath(item.GetType().FullName + item.Id, filename);
            }

            var parentPath = Path.GetDirectoryName(path);

            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }

            return path;
        }

        private string GetSavePathForItemInMixedFolder(BaseItem item, ImageType type, string imageFilename, string extension)
        {
            if (type == ImageType.Primary)
            {
                return Path.ChangeExtension(item.Path, extension);
            }
            var folder = Path.GetDirectoryName(item.Path);

            return Path.Combine(folder, Path.GetFileNameWithoutExtension(item.Path) + "-" + imageFilename);
        }
    }
}
