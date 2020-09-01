#pragma warning disable CA2227

using System;
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
        /// <param name="itemMetadata">The metadata.</param>
        public PersonRole(PersonRoleType type, ItemMetadata itemMetadata)
        {
            Type = type;

            if (itemMetadata == null)
            {
                throw new ArgumentNullException(nameof(itemMetadata));
            }

            itemMetadata.PersonRoles.Add(this);

            Sources = new HashSet<MetadataProviderId>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PersonRole"/> class.
        /// </summary>
        /// <remarks>
        /// Default constructor. Protected due to required properties, but present because EF needs it.
        /// </remarks>
        protected PersonRole()
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
        /// Gets or sets the name of the person's role.
        /// </summary>
        /// <remarks>
        /// Max length = 1024.
        /// </remarks>
        [MaxLength(1024)]
        [StringLength(1024)]
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the person's role type.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public PersonRoleType Type { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; protected set; }

        /// <summary>
        /// Gets or sets the person.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        [Required]
        public virtual Person Person { get; set; }

        /// <inheritdoc />
        public virtual ICollection<Artwork> Artwork { get; protected set; }

        /// <summary>
        /// Gets or sets a collection containing the metadata sources for this person role.
        /// </summary>
        public virtual ICollection<MetadataProviderId> Sources { get; protected set; }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
