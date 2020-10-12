#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Configuration
{
    public class MetadataPluginSummary
    {
        public MetadataPluginSummary()
        {
            SupportedImageTypes = Array.Empty<ImageType>();
            Plugins = Array.Empty<MetadataPlugin>();
        }

        /// <summary>
        ///     Gets or sets the type of the item.
        /// </summary>
        /// <value>The type of the item.</value>
        public string ItemType { get; set; }

        /// <summary>
        ///     Gets or sets the plugins.
        /// </summary>
        /// <value>The plugins.</value>
        public IReadOnlyCollection<MetadataPlugin> Plugins { get; set; }

        /// <summary>
        ///     Gets or sets the supported image types.
        /// </summary>
        /// <value>The supported image types.</value>
        public IReadOnlyCollection<ImageType> SupportedImageTypes { get; set; }
    }
}
