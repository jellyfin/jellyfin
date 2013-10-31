using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers
{
    /// <summary>
    /// Provides images for generic types by looking for standard images in the IBN
    /// </summary>
    public class ImagesByNameProvider : ImageFromMediaLocationProvider
    {
        private readonly IFileSystem _fileSystem;
        
        public ImagesByNameProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            _fileSystem = fileSystem;
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
            //only run for these generic types since we are expensive in file i/o
            return item is IndexFolder || item is BasePluginFolder || item is CollectionFolder;
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
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            // Force a refresh if the IBN path changed
            if (providerInfo.FileStamp != ConfigurationManager.ApplicationPaths.ItemsByNamePath.GetMD5())
            {
                return true;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
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

            var directoryInfo = new DirectoryInfo(location);

            if (!directoryInfo.Exists)
            {
                return DateTime.MinValue;
            }

            var files = directoryInfo.EnumerateFiles().ToList();

            if (files.Count == 0)
            {
                return DateTime.MinValue;
            }

            return files.Select(f =>
            {
                var lastWriteTime = _fileSystem.GetLastWriteTimeUtc(f);
                var creationTime = _fileSystem.GetCreationTimeUtc(f);

                return creationTime > lastWriteTime ? creationTime : lastWriteTime;

            }).Max();
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            var result = await base.FetchAsync(item, force, cancellationToken).ConfigureAwait(false);

            BaseProviderInfo data;

            if (!item.ProviderData.TryGetValue(Id, out data))
            {
                data = new BaseProviderInfo();
                item.ProviderData[Id] = data;
            }

            data.FileStamp = ConfigurationManager.ApplicationPaths.ItemsByNamePath.GetMD5();
            SetLastRefreshed(item, DateTime.UtcNow);
     
            return result;
        }

        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected string GetLocation(BaseItem item)
        {
            var name = _fileSystem.GetValidFilename(item.Name);

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

            var directoryInfo = new DirectoryInfo(location);

            if (!directoryInfo.Exists)
            {
                return null;
            }

            var files = directoryInfo.EnumerateFiles("*", SearchOption.TopDirectoryOnly).ToList();

            var file = files.FirstOrDefault(i => string.Equals(i.Name, filenameWithoutExtension + ".png", StringComparison.OrdinalIgnoreCase));

            if (file != null)
            {
                return file;
            }

            file = files.FirstOrDefault(i => string.Equals(i.Name, filenameWithoutExtension + ".jpg", StringComparison.OrdinalIgnoreCase));

            if (file != null)
            {
                return file;
            }

            return null;
        }
    }
}
