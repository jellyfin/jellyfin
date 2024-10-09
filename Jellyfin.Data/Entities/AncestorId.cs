using System;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Represents the relational informations for an <see cref="BaseItemEntity"/>.
/// </summary>
public class AncestorId
{
    /// <summary>
    /// Gets or Sets the AncestorId that may or may not be an database managed Item or an materialised local item.
    /// </summary>
    public required Guid ParentItemId { get; set; }

    /// <summary>
    /// Gets or Sets the related that may or may not be an database managed Item or an materialised local item.
    /// </summary>
    public required Guid ItemId { get; set; }
}
