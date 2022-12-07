using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a person.
    /// </summary>
    public class Person : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Person"/> class.
        /// </summary>
        /// <param name="name">The name of the person.</param>
        public Person(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            Name = name;
            DateAdded = DateTime.UtcNow;
            DateModified = DateAdded;

            Sources = new HashSet<MetadataProviderId>();
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

        /// <summary>
        /// Gets or sets the source id.
        /// </summary>
        /// <remarks>
        /// Max length = 255.
        /// </remarks>
        [MaxLength(256)]
        [StringLength(256)]
        public string? SourceId { get; set; }

        /// <summary>
        /// Gets the date added.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public DateTime DateAdded { get; private set; }

        /// <summary>
        /// Gets or sets the date modified.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public DateTime DateModified { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <summary>
        /// Gets a list of metadata sources for this person.
        /// </summary>
        public virtual ICollection<MetadataProviderId> Sources { get; private set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
