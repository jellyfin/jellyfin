using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a release for a library item, eg. Director's cut vs. standard.
    /// </summary>
    public class Release : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Release"/> class.
        /// </summary>
        /// <param name="name">The name of this release.</param>
        public Release(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            Name = name;

            MediaFiles = new HashSet<MediaFile>();
            Chapters = new HashSet<Chapter>();
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
        /// Gets or sets the name.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 1024.
        /// </remarks>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Name { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <summary>
        /// Gets a collection containing the media files for this release.
        /// </summary>
        public virtual ICollection<MediaFile> MediaFiles { get; private set; }

        /// <summary>
        /// Gets a collection containing the chapters for this release.
        /// </summary>
        public virtual ICollection<Chapter> Chapters { get; private set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
