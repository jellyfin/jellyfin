using System;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
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
        }

        /// <inheritdoc />
        public bool IsMetadataFetcherEnabled(BaseItem baseItem, TypeOptions? libraryTypeOptions, string name)
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

            if (libraryTypeOptions is not null)
            {
                return libraryTypeOptions.MetadataFetchers.Contains(name, StringComparison.OrdinalIgnoreCase);
            }

            var itemConfig = _serverConfigurationManager.GetMetadataOptionsForType(baseItem.GetType().Name);
            return itemConfig is null || !itemConfig.DisabledMetadataFetchers.Contains(name, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool IsImageFetcherEnabled(BaseItem baseItem, TypeOptions? libraryTypeOptions, string name)
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

            if (libraryTypeOptions is not null)
            {
                return libraryTypeOptions.ImageFetchers.Contains(name, StringComparison.OrdinalIgnoreCase);
            }

            var itemConfig = _serverConfigurationManager.GetMetadataOptionsForType(baseItem.GetType().Name);
            return itemConfig is null || !itemConfig.DisabledImageFetchers.Contains(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
