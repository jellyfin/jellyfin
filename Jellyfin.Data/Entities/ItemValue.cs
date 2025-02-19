using System;
using System.Collections.Generic;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Represents an ItemValue for a BaseItem.
/// </summary>
public class ItemValue
{
    /// <summary>
    /// Gets or Sets the ItemValueId.
    /// </summary>
    public required Guid ItemValueId { get; set; }

    /// <summary>
    /// Gets or Sets the Type.
    /// </summary>
    public required ItemValueType Type { get; set; }

    /// <summary>
    /// Gets or Sets the Value.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or Sets the sanitized Value.
    /// </summary>
    public required string CleanValue { get; set; }

    /// <summary>
    /// Gets all associated BaseItems.
    /// </summary>
    public required ICollection<ItemValueMap> BaseItemsMap { get; init; }
}
