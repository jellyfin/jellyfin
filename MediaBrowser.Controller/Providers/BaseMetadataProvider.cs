using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Class BaseMetadataProvider
    /// </summary>
    public abstract class BaseMetadataProvider
    {
        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; set; }

        protected ILogManager LogManager { get; set; }

        /// <summary>
        /// Gets the configuration manager.
        /// </summary>
        /// <value>The configuration manager.</value>
        protected IServerConfigurationManager ConfigurationManager { get; private set; }

        /// <summary>
        /// The _id
        /// </summary>
        public readonly Guid Id;

        /// <summary>
        /// The true task result
        /// </summary>
        protected static readonly Task<bool> TrueTaskResult = Task.FromResult(true);

        protected static readonly Task<bool> FalseTaskResult = Task.FromResult(false);

        protected static readonly SemaphoreSlim XmlParsingResourcePool = new SemaphoreSlim(4, 4);

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public abstract bool Supports(BaseItem item);

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public virtual bool RequiresInternet
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the provider version.
        /// </summary>
        /// <value>The provider version.</value>
        protected virtual string ProviderVersion
        {
            get
            {
                return null;
            }
        }

        public virtual ItemUpdateType ItemUpdateType
        {
            get { return RequiresInternet ? ItemUpdateType.MetadataDownload : ItemUpdateType.MetadataImport; }
        }

        /// <summary>
        /// Gets a value indicating whether [refresh on version change].
        /// </summary>
        /// <value><c>true</c> if [refresh on version change]; otherwise, <c>false</c>.</value>
        protected virtual bool RefreshOnVersionChange
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if this provider is relatively slow and, therefore, should be skipped
        /// in certain instances.  Default is whether or not it requires internet.  Can be overridden
        /// for explicit designation.
        /// </summary>
        /// <value><c>true</c> if this instance is slow; otherwise, <c>false</c>.</value>
        public virtual bool IsSlow
        {
            get { return RequiresInternet; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseMetadataProvider" /> class.
        /// </summary>
        protected BaseMetadataProvider(ILogManager logManager, IServerConfigurationManager configurationManager)
        {
            Logger = logManager.GetLogger(GetType().Name);
            LogManager = logManager;
            ConfigurationManager = configurationManager;
            Id = GetType().FullName.GetMD5();

            Initialize();
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected virtual void Initialize()
        {
        }

        /// <summary>
        /// Sets the persisted last refresh date on the item for this provider.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="value">The value.</param>
        /// <param name="providerVersion">The provider version.</param>
        /// <param name="providerInfo">The provider information.</param>
        /// <param name="status">The status.</param>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public virtual void SetLastRefreshed(BaseItem item, DateTime value, string providerVersion,
            BaseProviderInfo providerInfo, ProviderRefreshStatus status = ProviderRefreshStatus.Success)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            providerInfo.LastRefreshed = value;
            providerInfo.LastRefreshStatus = status;
            providerInfo.ProviderVersion = providerVersion;

            // Save the file system stamp for future comparisons
            if (RefreshOnFileSystemStampChange && item.LocationType == LocationType.FileSystem)
            {
                try
                {
                    providerInfo.FileStamp = GetCurrentFileSystemStamp(item);
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error getting file stamp for {0}", ex, item.Path);
                }
            }
        }

        /// <summary>
        /// Sets the last refreshed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="value">The value.</param>
        /// <param name="providerInfo">The provider information.</param>
        /// <param name="status">The status.</param>
        public void SetLastRefreshed(BaseItem item, DateTime value,
            BaseProviderInfo providerInfo, ProviderRefreshStatus status = ProviderRefreshStatus.Success)
        {
            SetLastRefreshed(item, value, ProviderVersion, providerInfo, status);
        }

        /// <summary>
        /// Returns whether or not this provider should be re-fetched.  Default functionality can
        /// compare a provided date with a last refresh time.  This can be overridden for more complex
        /// determinations.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public bool NeedsRefresh(BaseItem item, BaseProviderInfo data)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            return NeedsRefreshInternal(item, data);
        }

        /// <summary>
        /// Gets a value indicating whether [enforce dont fetch metadata].
        /// </summary>
        /// <value><c>true</c> if [enforce dont fetch metadata]; otherwise, <c>false</c>.</value>
        public virtual bool EnforceDontFetchMetadata
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        protected virtual bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (providerInfo == null)
            {
                throw new ArgumentNullException("providerInfo");
            }

            if (providerInfo.LastRefreshed == default(DateTime))
            {
                return true;
            }
            
            if (NeedsRefreshBasedOnCompareDate(item, providerInfo))
            {
                return true;
            }

            if (RefreshOnFileSystemStampChange && item.LocationType == LocationType.FileSystem &&
                HasFileSystemStampChanged(item, providerInfo))
            {
                return true;
            }

            if (RefreshOnVersionChange && !String.Equals(ProviderVersion, providerInfo.ProviderVersion))
            {
                return true;
            }

            if (providerInfo.LastRefreshStatus != ProviderRefreshStatus.Success)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Needses the refresh based on compare date.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected virtual bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            return CompareDate(item) > providerInfo.LastRefreshed;
        }

        /// <summary>
        /// Determines if the item's file system stamp has changed from the last time the provider refreshed
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if [has file system stamp changed] [the specified item]; otherwise, <c>false</c>.</returns>
        protected bool HasFileSystemStampChanged(BaseItem item, BaseProviderInfo providerInfo)
        {
            return GetCurrentFileSystemStamp(item) != providerInfo.FileStamp;
        }

        /// <summary>
        /// Override this to return the date that should be compared to the last refresh date
        /// to determine if this provider should be re-fetched.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>DateTime.</returns>
        protected virtual DateTime CompareDate(BaseItem item)
        {
            return DateTime.MinValue.AddMinutes(1); // want this to be greater than mindate so new items will refresh
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="providerInfo">The provider information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public abstract Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public abstract MetadataProviderPriority Priority { get; }

        /// <summary>
        /// Returns true or false indicating if the provider should refresh when the contents of it's directory changes
        /// </summary>
        /// <value><c>true</c> if [refresh on file system stamp change]; otherwise, <c>false</c>.</value>
        protected virtual bool RefreshOnFileSystemStampChange
        {
            get
            {
                return false;
            }
        }

        protected virtual string[] FilestampExtensions
        {
            get { return new string[] { }; }
        }

        /// <summary>
        /// Determines if the parent's file system stamp should be used for comparison
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected virtual bool UseParentFileSystemStamp(BaseItem item)
        {
            // True when the current item is just a file
            return !item.ResolveArgs.IsDirectory;
        }

        protected virtual IEnumerable<BaseItem> GetItemsForFileStampComparison(BaseItem item)
        {
            if (UseParentFileSystemStamp(item) && item.Parent != null)
            {
                return new[] { item.Parent };
            }

            return new[] { item };
        }

        /// <summary>
        /// Gets the item's current file system stamp
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Guid.</returns>
        private Guid GetCurrentFileSystemStamp(BaseItem item)
        {
            return GetFileSystemStamp(GetItemsForFileStampComparison(item));
        }

        private Dictionary<string, string> _fileStampExtensionsDictionary;

        private Dictionary<string, string> FileStampExtensionsDictionary
        {
            get
            {
                return _fileStampExtensionsDictionary ??
                       (_fileStampExtensionsDictionary =
                           FilestampExtensions.ToDictionary(i => i, StringComparer.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Gets the file system stamp.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>Guid.</returns>
        protected virtual Guid GetFileSystemStamp(IEnumerable<BaseItem> items)
        {
            var sb = new StringBuilder();

            var extensions = FileStampExtensionsDictionary;
            var numExtensions = FilestampExtensions.Length;

            foreach (var item in items)
            {
                // If there's no path or the item is a file, there's nothing to do
                if (item.LocationType == LocationType.FileSystem)
                {
                    var resolveArgs = item.ResolveArgs;

                    if (resolveArgs.IsDirectory)
                    {
                        // Record the name of each file 
                        // Need to sort these because accoring to msdn docs, our i/o methods are not guaranteed in any order
                        AddFiles(sb, resolveArgs.FileSystemChildren, extensions, numExtensions);
                        AddFiles(sb, resolveArgs.MetadataFiles, extensions, numExtensions);
                    }
                }
            }

            var stamp = sb.ToString();

            if (string.IsNullOrEmpty(stamp))
            {
                return Guid.Empty;
            }

            return stamp.GetMD5();
        }

        private static readonly Dictionary<string, string> FoldersToMonitor = new[] { "extrafanart", "extrathumbs" }
            .ToDictionary(i => i, StringComparer.OrdinalIgnoreCase);

        protected Guid GetFileSystemStamp(IEnumerable<FileSystemInfo> files)
        {
            var sb = new StringBuilder();

            var extensions = FileStampExtensionsDictionary;
            var numExtensions = FilestampExtensions.Length;

            // Record the name of each file 
            // Need to sort these because accoring to msdn docs, our i/o methods are not guaranteed in any order
            AddFiles(sb, files, extensions, numExtensions);

            return sb.ToString().GetMD5();
        }

        /// <summary>
        /// Adds the files.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="files">The files.</param>
        /// <param name="extensions">The extensions.</param>
        /// <param name="numExtensions">The num extensions.</param>
        private void AddFiles(StringBuilder sb, IEnumerable<FileSystemInfo> files, Dictionary<string, string> extensions, int numExtensions)
        {
            foreach (var file in files
                .OrderBy(f => f.Name))
            {
                try
                {
                    if ((file.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (FoldersToMonitor.ContainsKey(file.Name))
                        {
                            sb.Append(file.Name);

                            var children = ((DirectoryInfo)file).EnumerateFiles("*", SearchOption.TopDirectoryOnly).ToList();
                            AddFiles(sb, children, extensions, numExtensions);
                        }
                    }

                    // It's a file
                    else if (numExtensions == 0 || extensions.ContainsKey(file.Extension))
                    {
                        sb.Append(file.Name);
                    }
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error accessing file attributes for {0}", ex, file.FullName);
                }
            }
        }
    }
}
