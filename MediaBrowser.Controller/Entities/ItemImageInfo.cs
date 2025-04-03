#pragma warning disable CS1591

using System;
using System.Text.Json.Serialization;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    public class ItemImageInfo
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public required string Path { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public ImageType Type { get; set; }

        /// <summary>
        /// Gets or sets the date modified.
        /// </summary>
        /// <value>The date modified.</value>
        public DateTime DateModified { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the blurhash.
        /// </summary>
        /// <value>The blurhash.</value>
        public string? BlurHash { get; set; }

        /// <summary>
        /// Gets or sets the file hash.
        /// </summary>
        /// <value>The file hash.</value>
        public string? FileHash { get; set; }

        [JsonIgnore]
        public bool IsLocalFile => !Path.StartsWith("http", StringComparison.OrdinalIgnoreCase);
    }
}
