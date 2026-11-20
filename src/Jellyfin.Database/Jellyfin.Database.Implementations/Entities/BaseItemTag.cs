using System;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// Represents a tag a BaseItem carries.
/// </summary>
public class BaseItemTag
{
    /// <summary>
    /// Gets or Sets the reference ItemId.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets the reference BaseItem.
    /// </summary>
    public required BaseItemEntity Item { get; set; }

    /// <summary>
    /// Gets or Sets the tag as written.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or Sets the sanitized tag.
    /// </summary>
    public required string CleanValue { get; set; }
}
