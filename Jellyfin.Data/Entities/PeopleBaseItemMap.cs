using System;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Mapping table for People to BaseItems.
/// </summary>
public class PeopleBaseItemMap
{
    /// <summary>
    /// Gets or Sets the SortOrder.
    /// </summary>
    public int? SortOrder { get; set; }

    /// <summary>
    /// Gets or Sets the ListOrder.
    /// </summary>
    public int? ListOrder { get; set; }

    /// <summary>
    /// Gets or Sets the Role name the assosiated actor played in the <see cref="BaseItemEntity"/>.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Gets or Sets The ItemId.
    /// </summary>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets Reference Item.
    /// </summary>
    public required BaseItemEntity Item { get; set; }

    /// <summary>
    /// Gets or Sets The PeopleId.
    /// </summary>
    public required Guid PeopleId { get; set; }

    /// <summary>
    /// Gets or Sets Reference People.
    /// </summary>
    public required People People { get; set; }
}
