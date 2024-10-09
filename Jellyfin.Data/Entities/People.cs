using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities;

/// <summary>
/// People entity.
/// </summary>
public class People
{
    /// <summary>
    /// Gets or Sets The ItemId.
    /// </summary>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets Reference Item.
    /// </summary>
    public required BaseItemEntity Item { get; set; }

    /// <summary>
    /// Gets or Sets the Persons Name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or Sets the Role.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Gets or Sets the Type.
    /// </summary>
    public string? PersonType { get; set; }

    /// <summary>
    /// Gets or Sets the SortOrder.
    /// </summary>
    public int? SortOrder { get; set; }

    /// <summary>
    /// Gets or Sets the ListOrder.
    /// </summary>
    public int? ListOrder { get; set; }
}
