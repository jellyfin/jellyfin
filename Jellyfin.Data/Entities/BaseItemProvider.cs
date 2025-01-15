using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Jellyfin.Data.Entities;

/// <summary>
/// Represents a Key-Value relation of an BaseItem's provider.
/// </summary>
public class BaseItemProvider
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
    /// Gets or Sets the ProvidersId.
    /// </summary>
    public required string ProviderId { get; set; }

    /// <summary>
    /// Gets or Sets the Providers Value.
    /// </summary>
    public required string ProviderValue { get; set; }
}
