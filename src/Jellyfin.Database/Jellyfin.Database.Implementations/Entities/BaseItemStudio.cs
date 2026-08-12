using System;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// Links a BaseItem to the studio item one of its studios belongs to.
/// </summary>
public class BaseItemStudio
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
    /// Gets or Sets the id of the studio item.
    /// </summary>
    public Guid StudioItemId { get; set; }
}
