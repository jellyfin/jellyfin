using System;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// Represents a linked child relationship between items (e.g., BoxSet to Movies, Playlist to tracks).
/// </summary>
public class LinkedChildEntity
{
    /// <summary>
    /// Gets or sets the parent item ID (BoxSet, Playlist, etc.).
    /// </summary>
    public required Guid ParentId { get; set; }

    /// <summary>
    /// Gets or sets the child item ID.
    /// </summary>
    public required Guid ChildId { get; set; }

    /// <summary>
    /// Gets or sets the type of linked child (Manual or Shortcut).
    /// </summary>
    public required LinkedChildType ChildType { get; set; }

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public int? SortOrder { get; set; }

    /// <summary>
    /// Gets or sets the parent item navigation property.
    /// </summary>
    public BaseItemEntity? Parent { get; set; }

    /// <summary>
    /// Gets or sets the child item navigation property.
    /// </summary>
    public BaseItemEntity? Child { get; set; }
}
