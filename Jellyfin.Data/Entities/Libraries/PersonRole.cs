using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities.Libraries
{
    /// <summary>
    /// An entity representing a person's role in media.
    /// </summary>
    public class PersonRole : IHasArtwork, IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PersonRole"/> class.
        /// </summary>
        /// <param name="type">The role type.</param>
        /// <param name="person">The person.</param>
        public PersonRole(PersonRoleType type, Person person)
        {
            Type = type;
            Person = person;
            Artwork = new HashSet<Artwork>();
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
        /// Gets or sets the name of the person's role.
        /// </summary>
        /// <remarks>
        /// Max length = 1024.
        /// </remarks>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string? Role { get; set; }

        /// <summary>
        /// Gets or sets the person's role type.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public PersonRoleType Type { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <summary>
        /// Gets or sets the person.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public virtual Person Person { get; set; }

        /// <inheritdoc />
        public virtual ICollection<Artwork> Artwork { get; private set; }

        /// <summary>
        /// Gets a collection containing the metadata sources for this person role.
        /// </summary>
        public virtual ICollection<MetadataProviderId> Sources { get; private set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
