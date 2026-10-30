using System;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// Links a BaseItem to the genre item one of its genres belongs to.
/// </summary>
public class BaseItemGenre
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
    /// Gets or Sets the id of the genre item.
    /// </summary>
    public Guid GenreItemId { get; set; }
}
