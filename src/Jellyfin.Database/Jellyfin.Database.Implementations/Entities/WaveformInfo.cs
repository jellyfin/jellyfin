using System;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// An entity representing waveform metadata for an audio item.
/// </summary>
public class WaveformInfo
{
    /// <summary>
    /// Gets or sets the id of the associated item.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the item reference.
    /// </summary>
    public BaseItemEntity? Item { get; set; }

    /// <summary>
    /// Gets or sets the number of waveform samples per second.
    /// </summary>
    public int SamplesPerSecond { get; set; }
}
