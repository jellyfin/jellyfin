using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Jellyfin.Data.Entities;

/// <summary>
/// An entity representing the metadata for a group of trickplay tiles.
/// </summary>
public class AudioWaveformInfo
{
    /// <summary>
    /// Gets or sets the ID of the associated item.
    /// </summary>
    /// <remarks>
    /// Required. Acts as the primary key.
    /// </remarks>
    [Key]
    [JsonIgnore]
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the amount of values calculated per second.
    /// </summary>
    /// <remarks>
    /// Required.
    /// </remarks>
    public int SamplesPerSecond { get; set; }
}
