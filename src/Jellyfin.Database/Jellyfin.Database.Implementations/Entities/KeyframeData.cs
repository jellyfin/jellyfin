#pragma warning disable CA2227 // Collection properties should be read only

using System;
using System.Collections.Generic;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// Keyframe information for a specific file.
/// </summary>
public class KeyframeData
{
    /// <summary>
    /// Gets or Sets the ItemId.
    /// </summary>
    public required Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the total duration of the stream in ticks.
    /// </summary>
    public long TotalDuration { get; set; }

    /// <summary>
    /// Gets or sets the keyframes in ticks.
    /// </summary>
    public ICollection<long>? KeyframeTicks { get; set; }

    /// <summary>
    /// Gets or sets the item reference.
    /// </summary>
    public BaseItemEntity? Item { get; set; }
}
