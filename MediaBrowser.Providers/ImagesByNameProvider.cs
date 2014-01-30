using System.Collections.Generic;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Providers
{
    /// <summary>
    /// Provides images for generic types by looking for standard images in the IBN
    /// </summary>
    public class ImagesByNameProvider : ImageFromMediaLocationProvider
    {
        public ImagesByNameProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IFileSystem fileSystem)
            : base(logManager, configurationManager, fileSystem)
        {
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            // Only run for these generic types since we are expensive in file i/o
            return item is ICollectionFolder;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get
            {
                return MetadataProviderPriority.Last;
            }
        }

        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected string GetLocation(BaseItem item)
        {
            var name = FileSystem.GetValidFilename(item.Name);

            return Path.Combine(ConfigurationManager.ApplicationPaths.GeneralPath, name);
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        /// <param name="filenameWithoutExtension">The filename without extension.</param>
        /// <returns>FileSystemInfo.</returns>
        protected override FileSystemInfo GetImage(BaseItem item, ItemResolveArgs args, string filenameWithoutExtension)
        {
            var location = GetLocation(item);

            return GetImageFromLocation(location, filenameWithoutExtension);
        }

        protected override Guid GetFileSystemStamp(IEnumerable<BaseItem> items)
        {
            var location = GetLocation(items.First());

            try
            {
                var files = new DirectoryInfo(location)
                    .EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                    .Where(i =>
                    {
                        var ext = i.Extension;

                        return !string.IsNullOrEmpty(ext) &&
                            BaseItem.SupportedImageExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
                    })
                    .ToList();

                return GetFileSystemStamp(files);
            }
            catch (DirectoryNotFoundException)
            {
                // User doesn't have the folder. No need to log or blow up

                return Guid.Empty;
            }
        }
    }
}
