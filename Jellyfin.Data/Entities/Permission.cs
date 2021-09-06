#pragma warning disable CA1711 // Identifiers should not have incorrect suffix

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity representing whether the associated user has a specific permission.
    /// </summary>
    public class Permission : IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Permission"/> class.
        /// Public constructor with required data.
        /// </summary>
        /// <param name="kind">The permission kind.</param>
        /// <param name="value">The value of this permission.</param>
        public Permission(PermissionKind kind, bool value)
        {
            Kind = kind;
            Value = value;
        }

        /// <summary>
        /// Gets the id of this permission.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the id of the associated user.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <summary>
        /// Gets the type of this permission.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public PermissionKind Kind { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the associated user has this permission.
        /// </summary>
        /// <remarks>
        /// Required.
        /// </remarks>
        public bool Value { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <inheritdoc/>
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
