using System;
using System.Linq;
using System.Threading;
using MediaBrowser.Common.Configuration;
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

        private int _metadataRefreshConcurrency = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemManager"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        public BaseItemManager(IServerConfigurationManager serverConfigurationManager)
        {
            _serverConfigurationManager = serverConfigurationManager;

            _metadataRefreshConcurrency = GetMetadataRefreshConcurrency();
            SetupMetadataThrottler();

            _serverConfigurationManager.ConfigurationUpdated += OnConfigurationUpdated;
        }

        /// <inheritdoc />
        public SemaphoreSlim MetadataRefreshThrottler { get; private set; }

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

            var typeOptions = libraryOptions.GetTypeOptions(baseItem.GetType().Name);
            if (typeOptions != null)
            {
                return typeOptions.MetadataFetchers.Contains(name, StringComparer.OrdinalIgnoreCase);
            }

            if (!libraryOptions.EnableInternetProviders)
            {
                return false;
            }

            var itemConfig = _serverConfigurationManager.Configuration.MetadataOptions.FirstOrDefault(i => string.Equals(i.ItemType, GetType().Name, StringComparison.OrdinalIgnoreCase));

            return itemConfig == null || !itemConfig.DisabledMetadataFetchers.Contains(name, StringComparer.OrdinalIgnoreCase);
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

            var typeOptions = libraryOptions.GetTypeOptions(baseItem.GetType().Name);
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

        /// <summary>
        /// Called when the configuration is updated.
        /// It will refresh the metadata throttler if the relevant config changed.
        /// </summary>
        private void OnConfigurationUpdated(object sender, EventArgs e)
        {
            int newMetadataRefreshConcurrency = GetMetadataRefreshConcurrency();
            if (_metadataRefreshConcurrency != newMetadataRefreshConcurrency)
            {
                _metadataRefreshConcurrency = newMetadataRefreshConcurrency;
                SetupMetadataThrottler();
            }
        }

        /// <summary>
        /// Creates the metadata refresh throttler.
        /// </summary>
        private void SetupMetadataThrottler()
        {
            MetadataRefreshThrottler = new SemaphoreSlim(_metadataRefreshConcurrency);
        }

        /// <summary>
        /// Returns the metadata refresh concurrency.
        /// </summary>
        private int GetMetadataRefreshConcurrency()
        {
            var concurrency = _serverConfigurationManager.Configuration.LibraryMetadataRefreshConcurrency;

            if (concurrency <= 0)
            {
                concurrency = Environment.ProcessorCount;
            }

            return concurrency;
        }
    }
}
