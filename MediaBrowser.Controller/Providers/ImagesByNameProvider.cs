using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Provides images for generic types by looking for standard images in the IBN
    /// </summary>
    public class ImagesByNameProvider : ImageFromMediaLocationProvider
    {
        public ImagesByNameProvider(ILogManager logManager, IServerConfigurationManager configurationManager) : base(logManager, configurationManager)
        {
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            //only run for these generic types since we are expensive in file i/o
            return item is IndexFolder || item is BasePluginFolder;
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
        /// Gets a value indicating whether [refresh on file system stamp change].
        /// </summary>
        /// <value><c>true</c> if [refresh on file system stamp change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Override this to return the date that should be compared to the last refresh date
        /// to determine if this provider should be re-fetched.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>DateTime.</returns>
        protected override DateTime CompareDate(BaseItem item)
        {
            // If the IBN location exists return the last modified date of any file in it
            var location = GetLocation(item);
            return Directory.Exists(location) ? FileSystem.GetFiles(location).Select(f => f.CreationTimeUtc > f.LastWriteTimeUtc ? f.CreationTimeUtc : f.LastWriteTimeUtc).Max() : DateTime.MinValue;
        }

        /// <summary>
        /// The us culture
        /// </summary>
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected string GetLocation(BaseItem item)
        {
            var invalid = Path.GetInvalidFileNameChars();

            var name = item.Name ?? string.Empty;
            name = invalid.Aggregate(name, (current, c) => current.Replace(c.ToString(UsCulture), string.Empty));

            return Path.Combine(ConfigurationManager.ApplicationPaths.GeneralPath, name);
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="filenameWithoutExtension">The filename without extension.</param>
        /// <returns>System.Nullable{WIN32_FIND_DATA}.</returns>
        protected override WIN32_FIND_DATA? GetImage(BaseItem item, string filenameWithoutExtension)
        {
            var location = GetLocation(item);

            var result = FileSystem.GetFileData(Path.Combine(location, filenameWithoutExtension + ".png"));
            if (!result.HasValue)
                result = FileSystem.GetFileData(Path.Combine(location, filenameWithoutExtension + ".jpg"));

            return result;
        }
    }
}
