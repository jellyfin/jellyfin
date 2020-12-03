using System;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.BaseItemManager
{
    /// <inheritdoc />
    public class BaseItemManager : IBaseItemManager
    {
        private readonly IServerConfigurationManager _serverConfigurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemManager"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public BaseItemManager(IServerConfigurationManager serverConfigurationManager)
        {
            _serverConfigurationManager = serverConfigurationManager;

            MetadataRefreshThrottler = new Lazy<SemaphoreSlim>(() => {
                var concurrency = _serverConfigurationManager.Configuration.LibraryMetadataRefreshConcurrency;

                if (concurrency <= 0)
                {
                    concurrency = Environment.ProcessorCount;
                }

                return new SemaphoreSlim(concurrency);
            });
        }

        /// <inheritdoc />
        public Lazy<SemaphoreSlim> MetadataRefreshThrottler { get; private set; }

        /// <inheritdoc />
        public bool IsMetadataFetcherEnabled(BaseItem baseItem, LibraryOptions libraryOptions, string name)
        {
            if (baseItem is Channel)
            {
                // Hack alert.
                return true;
            }

            if (baseItem.SourceType == SourceType.Channel)
            {
                // Hack alert.
                return !baseItem.EnableMediaSourceDisplay;
            }

            var typeOptions = libraryOptions.GetTypeOptions(GetType().Name);
            if (typeOptions != null)
            {
                return typeOptions.ImageFetchers.Contains(name, StringComparer.OrdinalIgnoreCase);
            }

            if (!libraryOptions.EnableInternetProviders)
            {
                return false;
            }

            var itemConfig = _serverConfigurationManager.Configuration.MetadataOptions.FirstOrDefault(i => string.Equals(i.ItemType, GetType().Name, StringComparison.OrdinalIgnoreCase));

            return itemConfig == null || !itemConfig.DisabledImageFetchers.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool IsImageFetcherEnabled(BaseItem baseItem, LibraryOptions libraryOptions, string name)
        {
            if (baseItem is Channel)
            {
                // Hack alert.
                return true;
            }

            if (baseItem.SourceType == SourceType.Channel)
            {
                // Hack alert.
                return !baseItem.EnableMediaSourceDisplay;
            }

            var typeOptions = libraryOptions.GetTypeOptions(GetType().Name);
            if (typeOptions != null)
            {
                return typeOptions.ImageFetchers.Contains(name, StringComparer.OrdinalIgnoreCase);
            }

            if (!libraryOptions.EnableInternetProviders)
            {
                return false;
            }

            var itemConfig = _serverConfigurationManager.Configuration.MetadataOptions.FirstOrDefault(i => string.Equals(i.ItemType, GetType().Name, StringComparison.OrdinalIgnoreCase));

            return itemConfig == null || !itemConfig.DisabledImageFetchers.Contains(name, StringComparer.OrdinalIgnoreCase);
        }
    }
}
