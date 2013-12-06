using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers
{
    /// <summary>
    /// Provides images for all types by looking for standard images - folder, backdrop, logo, etc.
    /// </summary>
    public class ImageFromMediaLocationProvider : BaseMetadataProvider
    {
        protected readonly IFileSystem FileSystem;
        
        public ImageFromMediaLocationProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            FileSystem = fileSystem;
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.ImageUpdate;
            }
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            if (item.LocationType == LocationType.FileSystem)
            {
                if (item.ResolveArgs.IsDirectory)
                {
                    return true;
                }

                return item.IsInMixedFolder && item.Parent != null && !(item is Episode);
            }
            return false;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        /// <summary>
        /// Returns true or false indicating if the provider should refresh when the contents of it's directory changes
        /// </summary>
        /// <value><c>true</c> if [refresh on file system stamp change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the filestamp extensions.
        /// </summary>
        /// <value>The filestamp extensions.</value>
        protected override string[] FilestampExtensions
        {
            get
            {
                return BaseItem.SupportedImageExtensions;
            }
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Make sure current image paths still exist
            item.ValidateImages();

            cancellationToken.ThrowIfCancellationRequested();

            // Make sure current backdrop paths still exist
            item.ValidateBackdrops();

            var hasScreenshots = item as IHasScreenshots;
            if (hasScreenshots != null)
            {
                hasScreenshots.ValidateScreenshots();
            }

            cancellationToken.ThrowIfCancellationRequested();

            var args = GetResolveArgsContainingImages(item);

            PopulateBaseItemImages(item, args);

            SetLastRefreshed(item, DateTime.UtcNow);
            return TrueTaskResult;
        }

        private ItemResolveArgs GetResolveArgsContainingImages(BaseItem item)
        {
            if (item.IsInMixedFolder)
            {
                if (item.Parent == null)
                {
                    return item.ResolveArgs;
                }
                return item.Parent.ResolveArgs;
            }

            return item.ResolveArgs;
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        /// <param name="filenameWithoutExtension">The filename without extension.</param>
        /// <returns>FileSystemInfo.</returns>
        protected virtual FileSystemInfo GetImage(BaseItem item, ItemResolveArgs args, string filenameWithoutExtension)
        {
            return BaseItem.SupportedImageExtensions
                .Select(i => args.GetMetaFileByPath(GetFullImagePath(item, args, filenameWithoutExtension, i)))
                .FirstOrDefault(i => i != null);
        }

        protected virtual string GetFullImagePath(BaseItem item, ItemResolveArgs args, string filenameWithoutExtension, string extension)
        {
            var path = item.MetaLocation;

            if (item.IsInMixedFolder)
            {
                var pathFilenameWithoutExtension = Path.GetFileNameWithoutExtension(item.Path);

                // If the image filename and path file name match, just look for an image using the same full path as the item
                if (string.Equals(pathFilenameWithoutExtension, filenameWithoutExtension))
                {
                    return Path.ChangeExtension(item.Path, extension);
                }

                return Path.Combine(path, pathFilenameWithoutExtension + "-" + filenameWithoutExtension + extension);
            }

            return Path.Combine(path, filenameWithoutExtension + extension);
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Fills in image paths based on files win the folder
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        private void PopulateBaseItemImages(BaseItem item, ItemResolveArgs args)
        {
            PopulatePrimaryImage(item, args);

            // Logo Image
            var image = GetImage(item, args, "logo");

            if (image != null)
            {
                item.SetImage(ImageType.Logo, image.FullName);
            }

            // Clearart
            image = GetImage(item, args, "clearart");

            if (image != null)
            {
                item.SetImage(ImageType.Art, image.FullName);
            }

            // Disc
            image = GetImage(item, args, "disc") ??
                GetImage(item, args, "cdart");

            if (image != null)
            {
                item.SetImage(ImageType.Disc, image.FullName);
            }

            // Box Image
            image = GetImage(item, args, "box");

            if (image != null)
            {
                item.SetImage(ImageType.Box, image.FullName);
            }

            // BoxRear Image
            image = GetImage(item, args, "boxrear");

            if (image != null)
            {
                item.SetImage(ImageType.BoxRear, image.FullName);
            }

            // Thumbnail Image
            image = GetImage(item, args, "menu");

            if (image != null)
            {
                item.SetImage(ImageType.Menu, image.FullName);
            }

            PopulateBanner(item, args);
            PopulateThumb(item, args);

            // Backdrop Image
            PopulateBackdrops(item, args);
            PopulateScreenshots(item, args);
        }

        private void PopulatePrimaryImage(BaseItem item, ItemResolveArgs args)
        {
            // Primary Image
            var image = GetImage(item, args, "folder") ??
                GetImage(item, args, "poster") ??
                GetImage(item, args, "cover") ??
                GetImage(item, args, "default");

            // Support plex/xbmc convention
            if (image == null && item is Series)
            {
                image = GetImage(item, args, "show");
            }

            var isFileSystemItem = item.LocationType == LocationType.FileSystem;

            // Support plex/xbmc convention
            if (image == null && item is Season && item.IndexNumber.HasValue && isFileSystemItem)
            {
                var seasonMarker = item.IndexNumber.Value == 0
                                       ? "-specials"
                                       : item.IndexNumber.Value.ToString("00", _usCulture);

                // Get this one directly from the file system since we have to go up a level
                var filename = "season" + seasonMarker + "-poster";

                var path = Path.GetDirectoryName(item.Path);

                path = Path.Combine(path, filename);

                image = new FileInfo(path);

                if (!image.Exists)
                {
                    image = null;
                }
            }

            // Support plex/xbmc convention
            if (image == null && (item is Movie || item is MusicVideo || item is AdultVideo))
            {
                image = GetImage(item, args, "movie");
            }

            // Look for a file with the same name as the item
            if (image == null && isFileSystemItem)
            {
                var name = Path.GetFileNameWithoutExtension(item.Path);

                if (!string.IsNullOrEmpty(name))
                {
                    image = GetImage(item, args, name) ??
                        GetImage(item, args, name + "-poster");
                }
            }

            if (image != null)
            {
                item.SetImage(ImageType.Primary, image.FullName);
            }
        }

        /// <summary>
        /// Populates the banner.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        private void PopulateBanner(BaseItem item, ItemResolveArgs args)
        {
            // Banner Image
            var image = GetImage(item, args, "banner");

            if (image == null)
            {
                // Supprt xbmc conventions
                if (item is Season && item.IndexNumber.HasValue && item.LocationType == LocationType.FileSystem)
                {
                    var seasonMarker = item.IndexNumber.Value == 0
                                           ? "-specials"
                                           : item.IndexNumber.Value.ToString("00", _usCulture);

                    // Get this one directly from the file system since we have to go up a level
                    var filename = "season" + seasonMarker + "-banner";

                    var path = Path.GetDirectoryName(item.Path);

                    path = Path.Combine(path, filename);

                    image = new FileInfo(path);

                    if (!image.Exists)
                    {
                        image = null;
                    }
                }
            }

            if (image != null)
            {
                item.SetImage(ImageType.Banner, image.FullName);
            }
        }

        /// <summary>
        /// Populates the thumb.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        private void PopulateThumb(BaseItem item, ItemResolveArgs args)
        {
            // Thumbnail Image
            var image = GetImage(item, args, "thumb");

            if (image == null)
            {
                // Supprt xbmc conventions
                if (item is Season && item.IndexNumber.HasValue && item.LocationType == LocationType.FileSystem)
                {
                    var seasonMarker = item.IndexNumber.Value == 0
                                           ? "-specials"
                                           : item.IndexNumber.Value.ToString("00", _usCulture);

                    // Get this one directly from the file system since we have to go up a level
                    var filename = "season" + seasonMarker + "-landscape";

                    var path = Path.GetDirectoryName(item.Path);

                    path = Path.Combine(path, filename);

                    image = new FileInfo(path);

                    if (!image.Exists)
                    {
                        image = null;
                    }
                }
            }

            if (image != null)
            {
                item.SetImage(ImageType.Thumb, image.FullName);
            }

        }

        /// <summary>
        /// Populates the backdrops.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        private void PopulateBackdrops(BaseItem item, ItemResolveArgs args)
        {
            var isFileSystemItem = item.LocationType == LocationType.FileSystem;

            var backdropFiles = new List<string>();

            PopulateBackdrops(item, args, backdropFiles, "backdrop", "backdrop");

            // Support {name}-fanart.ext
            if (isFileSystemItem)
            {
                var name = Path.GetFileNameWithoutExtension(item.Path);

                if (!string.IsNullOrEmpty(name))
                {
                    var image = GetImage(item, args, name + "-fanart");

                    if (image != null)
                    {
                        backdropFiles.Add(image.FullName);
                    }
                }
            }

            // Support plex/xbmc conventions
            PopulateBackdrops(item, args, backdropFiles, "fanart", "fanart-");
            PopulateBackdrops(item, args, backdropFiles, "background", "background-");
            PopulateBackdrops(item, args, backdropFiles, "art", "art-");

            if (item is Season && item.IndexNumber.HasValue && isFileSystemItem)
            {
                var seasonMarker = item.IndexNumber.Value == 0
                                       ? "-specials"
                                       : item.IndexNumber.Value.ToString("00", _usCulture);

                // Get this one directly from the file system since we have to go up a level
                var filename = "season" + seasonMarker + "-fanart";

                var path = Path.GetDirectoryName(item.Path);

                path = Path.Combine(path, filename);

                var image = new FileInfo(path);

                if (image.Exists)
                {
                    backdropFiles.Add(image.FullName);
                }
            }

            if (isFileSystemItem)
            {
                PopulateBackdropsFromExtraFanart(args, backdropFiles);
            }

            if (backdropFiles.Count > 0)
            {
                item.BackdropImagePaths = backdropFiles;
            }
        }

        /// <summary>
        /// Populates the backdrops from extra fanart.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <param name="backdrops">The backdrops.</param>
        private void PopulateBackdropsFromExtraFanart(ItemResolveArgs args, List<string> backdrops)
        {
            if (!args.IsDirectory)
            {
                return;
            }

            if (args.ContainsFileSystemEntryByName("extrafanart"))
            {
                var path = Path.Combine(args.Path, "extrafanart");

                var imageFiles = Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly)
                    .Where(i =>
                    {
                        var extension = Path.GetExtension(i);

                        if (string.IsNullOrEmpty(extension))
                        {
                            return false;
                        }

                        return BaseItem.SupportedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
                    })
                    .ToList();

                backdrops.AddRange(imageFiles);
            }
        }

        /// <summary>
        /// Populates the backdrops.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        /// <param name="backdropFiles">The backdrop files.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="numberedSuffix">The numbered suffix.</param>
        private void PopulateBackdrops(BaseItem item, ItemResolveArgs args, List<string> backdropFiles, string filename, string numberedSuffix)
        {
            var image = GetImage(item, args, filename);

            if (image != null)
            {
                backdropFiles.Add(image.FullName);
            }

            var unfound = 0;
            for (var i = 1; i <= 20; i++)
            {
                // Backdrop Image
                image = GetImage(item, args, numberedSuffix + i);

                if (image != null)
                {
                    backdropFiles.Add(image.FullName);
                }
                else
                {
                    unfound++;

                    if (unfound >= 3)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Populates the screenshots.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        private void PopulateScreenshots(BaseItem item, ItemResolveArgs args)
        {
            // Screenshot Image
            var image = GetImage(item, args, "screenshot");

            var screenshotFiles = new List<string>();

            if (image != null)
            {
                screenshotFiles.Add(image.FullName);
            }

            var unfound = 0;
            for (var i = 1; i <= 20; i++)
            {
                // Screenshot Image
                image = GetImage(item, args, "screenshot" + i);

                if (image != null)
                {
                    screenshotFiles.Add(image.FullName);
                }
                else
                {
                    unfound++;

                    if (unfound >= 3)
                    {
                        break;
                    }
                }
            }

            if (screenshotFiles.Count > 0)
            {
                var hasScreenshots = item as IHasScreenshots;
                if (hasScreenshots != null)
                {
                    hasScreenshots.ScreenshotImagePaths = screenshotFiles;
                }
            }
        }

        protected FileSystemInfo GetImageFromLocation(string path, string filenameWithoutExtension)
        {
            try
            {
                var files = new DirectoryInfo(path)
                    .EnumerateFiles()
                    .Where(i =>
                    {
                        var fileName = Path.GetFileNameWithoutExtension(i.FullName);

                        if (!string.Equals(fileName, filenameWithoutExtension, StringComparison.OrdinalIgnoreCase))
                        {
                            return false;
                        }

                        var ext = i.Extension;

                        return !string.IsNullOrEmpty(ext) &&
                            BaseItem.SupportedImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
                    })
                    .ToList();

                return BaseItem.SupportedImageExtensions
                    .Select(ext => files.FirstOrDefault(i => string.Equals(ext, i.Extension, StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefault(file => file != null);
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }
    }
}
