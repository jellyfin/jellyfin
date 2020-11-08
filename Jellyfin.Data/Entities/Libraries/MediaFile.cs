#pragma warning disable CA2227

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
        /// <param name="release">The release.</param>
        public MediaFile(string path, MediaFileKind kind, Release release)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            Path = path;
            Kind = kind;

            if (release == null)
            {
                throw new ArgumentNullException(nameof(release));
            }

            release.MediaFiles.Add(this);

            MediaFileStreams = new HashSet<MediaFileStream>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaFile"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected MediaFile()
        {
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; protected set; }

        /// <summary>
        /// Gets or sets the path relative to the library root.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 65535.
        /// </remarks>
        [Required]
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
        public uint RowVersion { get; set; }

        /// <summary>
        /// Gets or sets a collection containing the streams in this file.
        /// </summary>
        public virtual ICollection<MediaFileStream> MediaFileStreams { get; protected set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
