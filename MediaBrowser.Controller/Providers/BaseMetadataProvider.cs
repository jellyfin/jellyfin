using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using System;
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
        protected Guid _id;
        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <value>The id.</value>
        public virtual Guid Id
        {
            get
            {
                if (_id == Guid.Empty) _id = GetType().FullName.GetMD5();
                return _id;
            }
        }

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
        /// <param name="status">The status.</param>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public virtual void SetLastRefreshed(BaseItem item, DateTime value, string providerVersion, ProviderRefreshStatus status = ProviderRefreshStatus.Success)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }
            
            var data = item.ProviderData.GetValueOrDefault(Id, new BaseProviderInfo { ProviderId = Id });
            data.LastRefreshed = value;
            data.LastRefreshStatus = status;
            data.ProviderVersion = providerVersion;

            // Save the file system stamp for future comparisons
            if (RefreshOnFileSystemStampChange)
            {
                data.FileSystemStamp = GetCurrentFileSystemStamp(item);
            }

            item.ProviderData[Id] = data;
        }

        /// <summary>
        /// Sets the last refreshed.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="value">The value.</param>
        /// <param name="status">The status.</param>
        public void SetLastRefreshed(BaseItem item, DateTime value, ProviderRefreshStatus status = ProviderRefreshStatus.Success)
        {
            SetLastRefreshed(item, value, ProviderVersion, status);
        }
        
        /// <summary>
        /// Returns whether or not this provider should be re-fetched.  Default functionality can
        /// compare a provided date with a last refresh time.  This can be overridden for more complex
        /// determinations.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public bool NeedsRefresh(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }

            var providerInfo = item.ProviderData.GetValueOrDefault(Id, new BaseProviderInfo());

            return NeedsRefreshInternal(item, providerInfo);
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
            
            if (CompareDate(item) > providerInfo.LastRefreshed)
            {
                return true;
            }

            if (RefreshOnFileSystemStampChange && HasFileSystemStampChanged(item, providerInfo))
            {
                return true;
            }

            if (RefreshOnVersionChange && !string.Equals(ProviderVersion, providerInfo.ProviderVersion))
            {
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Determines if the item's file system stamp has changed from the last time the provider refreshed
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if [has file system stamp changed] [the specified item]; otherwise, <c>false</c>.</returns>
        protected bool HasFileSystemStampChanged(BaseItem item, BaseProviderInfo providerInfo)
        {
            return GetCurrentFileSystemStamp(item) != providerInfo.FileSystemStamp;
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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public abstract Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken);

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

        /// <summary>
        /// Gets the item's current file system stamp
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Guid.</returns>
        private Guid GetCurrentFileSystemStamp(BaseItem item)
        {
            if (UseParentFileSystemStamp(item) && item.Parent != null)
            {
                return item.Parent.FileSystemStamp;
            }

            return item.FileSystemStamp;
        }
    }
}
