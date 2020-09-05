#pragma warning disable CA2227

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing artwork.
    /// </summary>
    public class Artwork : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Artwork"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="kind">The kind of art.</param>
        /// <param name="owner">The owner.</param>
        public Artwork(string path, ArtKind kind, IHasArtwork owner)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            Path = path;
            Kind = kind;

            owner?.Artwork.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Artwork"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected Artwork()
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
        /// Gets or sets the path.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 65535.
        /// </remarks>
        [Required]
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the kind of artwork.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public ArtKind Kind { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
