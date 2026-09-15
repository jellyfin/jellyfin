using System;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// Mapping table for Companies to BaseItems.
/// </summary>
public class CompanyBaseItemMap
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
    /// Gets or Sets The CompanyId.
    /// </summary>
    public required Guid CompanyId { get; set; }

    /// <summary>
    /// Gets or Sets Reference Company.
    /// </summary>
    public required CompanyEntity Company { get; set; }

    /// <summary>
    /// Gets or Sets what the company did for the item.
    /// </summary>
    public required CompanyKindEntity Type { get; set; }
}
