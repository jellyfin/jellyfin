using System;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// Represents an item in a user list.
/// </summary>
public class UserListItem
{
    /// <summary>
    /// Gets or sets the identifier of the user list.
    /// </summary>
    public required Guid UserListId { get; set; }

    /// <summary>
    /// Gets or sets the user list.
    /// </summary>
    public required UserList? UserList { get; set; }

    /// <summary>
    /// Gets or sets the custom data key.
    /// </summary>
    public required string CustomDataKey { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the item.
    /// </summary>
    public Guid? ItemId { get; set; }

    /// <summary>
    /// Gets or sets the item.
    /// </summary>
    public required BaseItemEntity? Item { get; set; }

    /// <summary>
    /// Gets or sets the date the item was added to the list.
    /// </summary>
    public DateTime DateAdded { get; set; }

    /// <summary>
    /// Gets or Sets the date the referenced <see cref="Item"/> has been deleted.
    /// </summary>
    public DateTime? RetentionDate { get; set; }

    /// <summary>
    /// Gets or sets the sort index of the item within the list.
    /// </summary>
    public int SortIndex { get; set; }
}
