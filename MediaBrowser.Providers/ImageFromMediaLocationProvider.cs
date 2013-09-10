using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
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
        public ImageFromMediaLocationProvider(ILogManager logManager, IServerConfigurationManager configurationManager)
            : base(logManager, configurationManager)
        {
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

            var args = GetResolveArgsContainingImages(item);

            // Make sure current image paths still exist
            item.ValidateImages();

            cancellationToken.ThrowIfCancellationRequested();

            // Make sure current backdrop paths still exist
            item.ValidateBackdrops();
            item.ValidateScreenshots();

            cancellationToken.ThrowIfCancellationRequested();

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

        /// <summary>
        /// Fills in image paths based on files win the folder
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        private void PopulateBaseItemImages(BaseItem item, ItemResolveArgs args)
        {
            // Primary Image
            var image = GetImage(item, args, "folder") ??
                GetImage(item, args, "poster") ??
                GetImage(item, args, "cover") ??
                GetImage(item, args, "default");

            // Look for a file with the same name as the item
            if (image == null)
            {
                var name = Path.GetFileNameWithoutExtension(item.Path);

                if (!string.IsNullOrEmpty(name))
                {
                    image = GetImage(item, args, name);
                }
            }

            if (image != null)
            {
                item.SetImage(ImageType.Primary, image.FullName);
            }

            // Logo Image
            image = GetImage(item, args, "logo");

            if (image != null)
            {
                item.SetImage(ImageType.Logo, image.FullName);
            }

            // Banner Image
            image = GetImage(item, args, "banner");

            if (image != null)
            {
                item.SetImage(ImageType.Banner, image.FullName);
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

            // Thumbnail Image
            image = GetImage(item, args, "thumb");

            if (image != null)
            {
                item.SetImage(ImageType.Thumb, image.FullName);
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

            // Backdrop Image
            PopulateBackdrops(item, args);

            // Screenshot Image
            image = GetImage(item, args, "screenshot");

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
                item.ScreenshotImagePaths = screenshotFiles;
            }
        }

        /// <summary>
        /// Populates the backdrops.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        private void PopulateBackdrops(BaseItem item, ItemResolveArgs args)
        {
            var backdropFiles = new List<string>();

            PopulateBackdrops(item, args, backdropFiles, "backdrop", "backdrop");

            // Support plex/xbmc conventions
            PopulateBackdrops(item, args, backdropFiles, "fanart", "fanart-");
            PopulateBackdrops(item, args, backdropFiles, "background", "background-");

            if (backdropFiles.Count > 0)
            {
                item.BackdropImagePaths = backdropFiles;
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
    }
}
