#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.IO
{
    public class FileSystemMetadata
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FileSystemMetadata"/> is exists.
        /// </summary>
        /// <value><c>true</c> if exists; otherwise, <c>false</c>.</value>
        public bool Exists { get; set; }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        /// <value>The full name.</value>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the extension.
        /// </summary>
        /// <value>The extension.</value>
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>The length.</value>
        public long Length { get; set; }

        /// <summary>
        /// Gets or sets the name of the directory.
        /// </summary>
        /// <value>The name of the directory.</value>
        public string DirectoryName { get; set; }

        /// <summary>
        /// Gets or sets the last write time UTC.
        /// </summary>
        /// <value>The last write time UTC.</value>
        public DateTime LastWriteTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets the creation time UTC.
        /// </summary>
        /// <value>The creation time UTC.</value>
        public DateTime CreationTimeUtc { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is directory.
        /// </summary>
        /// <value><c>true</c> if this instance is directory; otherwise, <c>false</c>.</value>
        public bool IsDirectory { get; set; }
    }
}
