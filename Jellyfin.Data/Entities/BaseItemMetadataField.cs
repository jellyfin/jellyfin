using System;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Enum MetadataFields.
/// </summary>
public class BaseItemMetadataField
{
    /// <summary>
    /// Gets or Sets Numerical ID of this enumeratable.
    /// </summary>
    public required int Id { get; set; }

    /// <summary>
    /// Gets or Sets all referenced <see cref="BaseItemEntity"/>.
    /// </summary>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or Sets all referenced <see cref="BaseItemEntity"/>.
    /// </summary>
    public required BaseItemEntity Item { get; set; }
}
