using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.BaseItemManager
{
    /// <inheritdoc />
    public class BaseItemManager : IBaseItemManager
    {
        private readonly IServerConfigurationManager _serverConfigurationManager;
        private readonly IProviderManager _providerManager;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemManager"/> class.
        /// </summary>
        /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
        /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
        /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
        public BaseItemManager(
            IServerConfigurationManager serverConfigurationManager,
            IProviderManager providerManager,
            IFileSystem fileSystem)
        {
            _serverConfigurationManager = serverConfigurationManager;
            _providerManager = providerManager;
            _fileSystem = fileSystem;
        }

        /// <inheritdoc />
        public bool IsMetadataFetcherEnabled(BaseItem baseItem, LibraryOptions libraryOptions, string name)
        {
            if (baseItem is Channel)
            {
                // hack alert
                return true;
            }

            if (baseItem.SourceType == SourceType.Channel)
            {
                // hack alert
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
                // hack alert
                return true;
            }

            if (baseItem.SourceType == SourceType.Channel)
            {
                // hack alert
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
        public void AddStudio(BaseItem baseItem, string studioName)
        {
            if (string.IsNullOrEmpty(studioName))
            {
                throw new ArgumentNullException(nameof(studioName));
            }

            var current = baseItem.Studios;

            if (!current.Contains(studioName, StringComparer.OrdinalIgnoreCase))
            {
                int curLen = current.Length;
                if (curLen == 0)
                {
                    baseItem.Studios = new[] { studioName };
                }
                else
                {
                    var newArr = new string[curLen + 1];
                    current.CopyTo(newArr, 0);
                    newArr[curLen] = studioName;
                    baseItem.Studios = newArr;
                }
            }
        }

        /// <inheritdoc />
        public void SetStudios(BaseItem baseItem, IEnumerable<string> studioNames)
        {
            baseItem.Studios = studioNames.Distinct().ToArray();
        }

        /// <inheritdoc />
        public void AddGenre(BaseItem baseItem, string genreName)
        {
            if (string.IsNullOrEmpty(genreName))
            {
                throw new ArgumentNullException(nameof(genreName));
            }

            var current = baseItem.Genres;
            if (!current.Contains(genreName, StringComparer.OrdinalIgnoreCase))
            {
                int curLen = current.Length;
                if (curLen == 0)
                {
                    baseItem.Genres = new[] { genreName };
                }
                else
                {
                    var newArr = new string[curLen + 1];
                    current.CopyTo(newArr, 0);
                    newArr[curLen] = genreName;
                    baseItem.Genres = newArr;
                }
            }
        }

        /// <inheritdoc />
        public void ChangedExternally(Guid baseItemId)
        {
            _providerManager.QueueRefresh(baseItemId, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), RefreshPriority.High);
        }
    }
}