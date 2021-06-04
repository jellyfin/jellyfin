using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a library.
    /// </summary>
    public class Library : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Library"/> class.
        /// </summary>
        /// <param name="name">The name of the library.</param>
        /// <param name="path">The path of the library.</param>
        public Library(string name, string path)
        {
            Name = name;
            Path = path;
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
        /// Required, Max length = 128.
        /// </remarks>
        [MaxLength(128)]
        [StringLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the root path of the library.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public string Path { get; set; }

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
