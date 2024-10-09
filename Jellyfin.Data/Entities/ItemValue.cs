using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Represents an ItemValue for a BaseItem.
/// </summary>
public class ItemValue
{
    /// <summary>
    /// Gets or Sets the reference ItemId.
    /// </summary>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets the referenced BaseItem.
    /// </summary>
    public required BaseItemEntity Item { get; set; }

    /// <summary>
    /// Gets or Sets the Type.
    /// </summary>
    public required ItemValueType Type { get; set; }

    /// <summary>
    /// Gets or Sets the Value.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or Sets the sanatised Value.
    /// </summary>
    public required string CleanValue { get; set; }
}
