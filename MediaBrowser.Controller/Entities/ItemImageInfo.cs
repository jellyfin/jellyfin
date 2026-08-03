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
        /// Gets or sets the remote URL this image was fetched from, if any.
        /// Used to detect when the source of a cached image changes.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the HTTP ETag of the remote source, used to detect content changes on refresh.
        /// </summary>
        public string? ETag { get; set; }

        /// <summary>
        /// Gets or sets the HTTP Last-Modified value of the remote source, used to detect content changes on refresh.
        /// </summary>
        public DateTime? SourceLastModified { get; set; }

        /// <summary>
        /// Gets a value indicating whether <see cref="Path"/> points at a file on disk rather than a remote URL.
        /// </summary>
        /// <value><c>true</c> when the image has been downloaded to local storage.</value>
        [JsonIgnore]
        public bool IsLocalFile => !Path.StartsWith("http", StringComparison.OrdinalIgnoreCase);
    }
}
