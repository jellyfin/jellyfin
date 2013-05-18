using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
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

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item.LocationType == LocationType.FileSystem && item.ResolveArgs.IsDirectory;
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
            ValidateImages(item);

            cancellationToken.ThrowIfCancellationRequested();

            // Make sure current backdrop paths still exist
            ValidateBackdrops(item);

            cancellationToken.ThrowIfCancellationRequested();

            PopulateBaseItemImages(item);

            SetLastRefreshed(item, DateTime.UtcNow);
            return TrueTaskResult;
        }

        /// <summary>
        /// Validates that images within the item are still on the file system
        /// </summary>
        /// <param name="item">The item.</param>
        private void ValidateImages(BaseItem item)
        {
            if (item.Images == null)
            {
                return;
            }

            // Only validate paths from the same directory - need to copy to a list because we are going to potentially modify the collection below
            var deletedKeys = item.Images.ToList().Where(image =>
            {
                var path = image.Value;

                return IsInMetaLocation(item, path) && item.ResolveArgs.GetMetaFileByPath(path) == null;

            }).Select(i => i.Key).ToList();

            // Now remove them from the dictionary
            foreach (var key in deletedKeys)
            {
                item.Images.Remove(key);
            }
        }

        /// <summary>
        /// Validates that backdrops within the item are still on the file system
        /// </summary>
        /// <param name="item">The item.</param>
        private void ValidateBackdrops(BaseItem item)
        {
            if (item.BackdropImagePaths == null)
            {
                return;
            }

            // Only validate paths from the same directory - need to copy to a list because we are going to potentially modify the collection below
            var deletedImages = item.BackdropImagePaths.Where(path => IsInMetaLocation(item, path) && item.ResolveArgs.GetMetaFileByPath(path) == null).ToList();

            // Now remove them from the dictionary
            foreach (var path in deletedImages)
            {
                item.BackdropImagePaths.Remove(path);
            }
        }

        /// <summary>
        /// Determines whether [is in same directory] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [is in same directory] [the specified item]; otherwise, <c>false</c>.</returns>
        private bool IsInMetaLocation(BaseItem item, string path)
        {
            return string.Equals(Path.GetDirectoryName(path), item.MetaLocation, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="filenameWithoutExtension">The filename without extension.</param>
        /// <returns>FileSystemInfo.</returns>
        protected virtual FileSystemInfo GetImage(BaseItem item, string filenameWithoutExtension)
        {
            return item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.ResolveArgs.Path, filenameWithoutExtension + ".png")) ?? item.ResolveArgs.GetMetaFileByPath(Path.Combine(item.ResolveArgs.Path, filenameWithoutExtension + ".jpg"));
        }

        /// <summary>
        /// Fills in image paths based on files win the folder
        /// </summary>
        /// <param name="item">The item.</param>
        private void PopulateBaseItemImages(BaseItem item)
        {
            var backdropFiles = new List<string>();
            var screenshotFiles = new List<string>();

            // Primary Image
            var image = GetImage(item, "folder");

            if (image != null)
            {
                item.SetImage(ImageType.Primary, image.FullName);
            }

            // Logo Image
            image = GetImage(item, "logo");

            if (image != null)
            {
                item.SetImage(ImageType.Logo, image.FullName);
            }

            // Banner Image
            image = GetImage(item, "banner");

            if (image != null)
            {
                item.SetImage(ImageType.Banner, image.FullName);
            }

            // Clearart
            image = GetImage(item, "clearart");

            if (image != null)
            {
                item.SetImage(ImageType.Art, image.FullName);
            }

            // Thumbnail Image
            image = GetImage(item, "thumb");

            if (image != null)
            {
                item.SetImage(ImageType.Thumb, image.FullName);
            }

            // Box Image
            image = GetImage(item, "box");

            if (image != null)
            {
                item.SetImage(ImageType.Box, image.FullName);
            }

            // BoxRear Image
            image = GetImage(item, "boxrear");

            if (image != null)
            {
                item.SetImage(ImageType.BoxRear, image.FullName);
            }

            // Thumbnail Image
            image = GetImage(item, "menu");

            if (image != null)
            {
                item.SetImage(ImageType.Menu, image.FullName);
            }

            // Backdrop Image
            image = GetImage(item, "backdrop");

            if (image != null)
            {
                backdropFiles.Add(image.FullName);
            }

            var unfound = 0;
            for (var i = 1; i <= 20; i++)
            {
                // Backdrop Image
                image = GetImage(item, "backdrop" + i);

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

            if (backdropFiles.Count > 0)
            {
                item.BackdropImagePaths = backdropFiles;
            }

            // Screenshot Image
            image = GetImage(item, "screenshot");

            if (image != null)
            {
                screenshotFiles.Add(image.FullName);
            }

            unfound = 0;
            for (var i = 1; i <= 20; i++)
            {
                // Screenshot Image
                image = GetImage(item, "screenshot" + i);

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
    }
}
