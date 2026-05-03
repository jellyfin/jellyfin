using System;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Options;

namespace MediaBrowser.Controller.BaseItemManager
{
    /// <inheritdoc />
    public class BaseItemManager : IBaseItemManager
    {
        private readonly IOptions<ServerConfiguration> _serverConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemManager"/> class.
        /// </summary>
        /// <param name="serverConfig">The server configuration.</param>
        public BaseItemManager(IOptions<ServerConfiguration> serverConfig)
        {
            _serverConfig = serverConfig;
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

            var itemConfig = Array.Find(_serverConfig.Value.MetadataOptions, i => string.Equals(i.ItemType, baseItem.GetType().Name, StringComparison.OrdinalIgnoreCase));
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

            var itemConfig = Array.Find(_serverConfig.Value.MetadataOptions, i => string.Equals(i.ItemType, baseItem.GetType().Name, StringComparison.OrdinalIgnoreCase));
            return itemConfig is null || !itemConfig.DisabledImageFetchers.Contains(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
