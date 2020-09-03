using System;
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
        public Library(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Library"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected Library()
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
        /// Required, Max length = 128.
        /// </remarks>
        [Required]
        [MaxLength(128)]
        [StringLength(128)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the root path of the library.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        [Required]
        public string Path { get; set; }

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
