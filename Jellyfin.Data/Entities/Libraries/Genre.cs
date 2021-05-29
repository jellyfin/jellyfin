using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a genre.
    /// </summary>
    public class Genre : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Genre"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public Genre(string name)
        {
            Name = name;
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
        /// Indexed, Required, Max length = 255.
        /// </remarks>
        [MaxLength(255)]
        [StringLength(255)]
        public string Name { get; set; }

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
