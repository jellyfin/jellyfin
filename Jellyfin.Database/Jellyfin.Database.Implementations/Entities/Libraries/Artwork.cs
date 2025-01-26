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
        public Artwork(string path, ArtKind kind)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            Path = path;
            Kind = kind;
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
        /// Gets or sets the path.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 65535.
        /// </remarks>
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
        public uint RowVersion { get; private set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
