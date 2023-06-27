#pragma warning disable CS1591

using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Library
{
    public static class MetadataConfigurationExtensions
    {
        public static MetadataConfiguration GetMetadataConfiguration(this IConfigurationManager config)
            => config.GetConfiguration<MetadataConfiguration>("metadata");

        /// <summary>
        /// Gets the <see cref="MetadataOptions" /> for the specified type.
        /// </summary>
        /// <param name="config">The <see cref="IServerConfigurationManager"/>.</param>
        /// <param name="type">The type to get the <see cref="MetadataOptions" /> for.</param>
        /// <returns>The <see cref="MetadataOptions" /> for the specified type or <c>null</c>.</returns>
        public static MetadataOptions? GetMetadataOptionsForType(this IServerConfigurationManager config, string type)
            => Array.Find(config.Configuration.MetadataOptions, i => string.Equals(i.ItemType, type, StringComparison.OrdinalIgnoreCase));
    }
}
