using System.Collections.Generic;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Interfaces
{
    /// <summary>
    /// An abstraction representing an entity that has permissions.
    /// </summary>
    public interface IHasPermissions
    {
        /// <summary>
        /// Gets a collection containing this entity's permissions.
        /// </summary>
        ICollection<Permission> Permissions { get; }

        /// <summary>
        /// Checks whether this entity has the specified permission kind.
        /// </summary>
        /// <param name="kind">The kind of permission.</param>
        /// <returns><c>true</c> if this entity has the specified permission, <c>false</c> otherwise.</returns>
        bool HasPermission(PermissionKind kind);

        /// <summary>
        /// Sets the specified permission to the provided value.
        /// </summary>
        /// <param name="kind">The kind of permission.</param>
        /// <param name="value">The value to set.</param>
        void SetPermission(PermissionKind kind, bool value);
    }
}
