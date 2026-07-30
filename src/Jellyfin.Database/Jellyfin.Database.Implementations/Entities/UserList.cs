using System;
using Jellyfin.Database.Implementations.Enums;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// Represents a named list belonging to a user.
/// </summary>
public class UserList
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who owns the list.
    /// </summary>
    public required Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the name of the list.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the kind of the list.
    /// </summary>
    public UserListKind Kind { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the user's default list.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether watched items should be removed from the list.
    /// </summary>
    public bool AutoRemoveWatched { get; set; }

    /// <summary>
    /// Gets or sets the sort index of the list.
    /// </summary>
    public int SortIndex { get; set; }

    /// <summary>
    /// Gets or sets the date the list was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// Gets or sets the date the list was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }
}
