using System;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Represents the relational information for an <see cref="BaseItemEntity"/>.
/// </summary>
public class AncestorId
{
    /// <summary>
    /// Gets or Sets the AncestorId.
    /// </summary>
    public required Guid ParentItemId { get; set; }

    /// <summary>
    /// Gets or Sets the related BaseItem.
    /// </summary>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets the ParentItem.
    /// </summary>
    public required BaseItemEntity ParentItem { get; set; }

    /// <summary>
    /// Gets or Sets the Child item.
    /// </summary>
    public required BaseItemEntity Item { get; set; }
}
