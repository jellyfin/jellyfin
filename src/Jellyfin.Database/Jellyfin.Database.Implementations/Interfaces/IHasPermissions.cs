using System.Collections.Generic;
using Jellyfin.Database.Implementations.Entities;

namespace Jellyfin.Database.Implementations.Interfaces;

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
