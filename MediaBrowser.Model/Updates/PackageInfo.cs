#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class PackageInfo.
    /// </summary>
    public class PackageInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageInfo"/> class.
        /// </summary>
        public PackageInfo()
        {
            Versions = Array.Empty<VersionInfo>();
            Id = string.Empty;
            Category = string.Empty;
            Name = string.Empty;
            Overview = string.Empty;
            Owner = string.Empty;
            Description = string.Empty;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a long description of the plugin containing features or helpful explanations.
        /// </summary>
        /// <value>The description.</value>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a short overview of what the plugin does.
        /// </summary>
        /// <value>The overview.</value>
        [JsonPropertyName("overview")]
        public string Overview { get; set; }

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        /// <value>The owner.</value>
        [JsonPropertyName("owner")]
        public string Owner { get; set; }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        /// <value>The category.</value>
        [JsonPropertyName("category")]
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the guid of the assembly associated with this plugin.
        /// This is used to identify the proper item for automatic updates.
        /// </summary>
        /// <value>The name.</value>
        [JsonPropertyName("guid")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the versions.
        /// </summary>
        /// <value>The versions.</value>
        [JsonPropertyName("versions")]
#pragma warning disable CA2227 // Collection properties should be read only
        public IList<VersionInfo> Versions { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the image url for the package.
        /// </summary>
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }
    }
}
