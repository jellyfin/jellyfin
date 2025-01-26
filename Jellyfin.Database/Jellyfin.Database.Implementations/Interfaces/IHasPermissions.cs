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
    }
}
