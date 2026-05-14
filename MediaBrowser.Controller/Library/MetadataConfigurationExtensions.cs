#pragma warning disable CS1591

using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Options;

namespace MediaBrowser.Controller.Library
{
    public static class MetadataConfigurationExtensions
    {
        /// <summary>
        /// Gets the <see cref="MetadataOptions" /> for the specified type.
        /// </summary>
        /// <param name="config">The Server Config.</param>
        /// <param name="type">The type to get the <see cref="MetadataOptions" /> for.</param>
        /// <returns>The <see cref="MetadataOptions" /> for the specified type or <c>null</c>.</returns>
        public static MetadataOptions? GetMetadataOptionsForType(this IOptions<ServerConfiguration> config, string type)
            => Array.Find(config.Value.MetadataOptions, i => string.Equals(i.ItemType, type, StringComparison.OrdinalIgnoreCase));
    }
}
