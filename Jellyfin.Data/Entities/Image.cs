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
    public class Image : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Image"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="type">The image type.</param>
        public Image(string path, ImageType type)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            Path = path;
            Type = type;
            LastModified = DateTime.UtcNow;
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
        /// Gets the user id.
        /// </summary>
        public Guid? UserId { get; private set; }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 65535.
        /// </remarks>
        [MaxLength(65535)]
        [StringLength(65535)]
        public string Path { get; private set; }

        /// <summary>
        /// Gets the image type.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public ImageType Type { get; private set; }

        /// <summary>
        /// Gets the blurhash string.
        /// </summary>
        public string? Blurhash { get; private set; }

        /// <summary>
        /// Gets the date last modified.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public DateTime LastModified { get; private set; }

        /// <inheritdoc/>
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
