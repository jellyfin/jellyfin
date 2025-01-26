using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a file on disk.
    /// </summary>
    public class MediaFile : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFile"/> class.
        /// </summary>
        /// <param name="path">The path relative to the LibraryRoot.</param>
        /// <param name="kind">The file kind.</param>
        public MediaFile(string path, MediaFileKind kind)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            Path = path;
            Kind = kind;

            MediaFileStreams = new HashSet<MediaFileStream>();
        }

        /// <summary>
        /// Gets the id.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the path relative to the library root.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 65535.
        /// </remarks>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the kind of media file.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public MediaFileKind Kind { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <summary>
        /// Gets a collection containing the streams in this file.
        /// </summary>
        public virtual ICollection<MediaFileStream> MediaFileStreams { get; private set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
