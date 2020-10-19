#pragma warning disable CA2227

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
        /// <param name="owner">The owner of this release.</param>
        public Release(string name, IHasReleases owner)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;

            owner?.Releases.Add(this);

            MediaFiles = new HashSet<MediaFile>();
            Chapters = new HashSet<Chapter>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Release"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected Release()
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
        /// Gets or sets the name.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 1024.
        /// </remarks>
        [Required]
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Name { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; set; }

        /// <summary>
        /// Gets or sets a collection containing the media files for this release.
        /// </summary>
        public virtual ICollection<MediaFile> MediaFiles { get; protected set; }

        /// <summary>
        /// Gets or sets a collection containing the chapters for this release.
        /// </summary>
        public virtual ICollection<Chapter> Chapters { get; protected set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
