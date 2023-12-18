using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Data.Interfaces;

namespace Jellyfin.Data.Entities
{
    /// <summary>
    /// An entity representing a group.
    /// </summary>
    public class Group : IHasPermissions, IHasConcurrencyToken
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Group"/> class.
        /// </summary>
        /// <param name="name">The name of the group.</param>
        public Group(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            Name = name;
            Id = Guid.NewGuid();

            Permissions = new HashSet<Permission>();
            Preferences = new HashSet<Preference>();
        }

        /// <summary>
        /// Gets the id of this group.
        /// </summary>
        /// <remarks>
        /// Identity, Indexed, Required.
        /// </remarks>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets the group's name.
        /// </summary>
        /// <remarks>
        /// Required, Max length = 255.
        /// </remarks>
        [MaxLength(255)]
        [StringLength(255)]
        public string Name { get; set; }

        /// <inheritdoc />
        [ConcurrencyCheck]
        public uint RowVersion { get; private set; }

        /// <summary>
        /// Gets a collection containing the group's permissions.
        /// </summary>
        public virtual ICollection<Permission> Permissions { get; private set; }

        /// <summary>
        /// Gets a collection containing the group's preferences.
        /// </summary>
        public virtual ICollection<Preference> Preferences { get; private set; }

        /// <inheritdoc/>
        public bool HasPermission(PermissionKind kind)
        {
            return Permissions.First(p => p.Kind == kind).Value;
        }

        /// <inheritdoc/>
        public void SetPermission(PermissionKind kind, bool value)
        {
            Permissions.First(p => p.Kind == kind).Value = value;
        }

        /// <inheritdoc />
        public void OnSavingChanges()
        {
            RowVersion++;
        }
    }
}
