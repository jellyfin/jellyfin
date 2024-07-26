using System;
using System.Text.Json.Serialization;

namespace Jellyfin.Data.Entities;

/// <summary>
///     An entity representing the metadata for a group of trickplay tiles.
/// </summary>
public class MediaSegment
{
    /// <summary>
    ///     Gets or sets the id of the media segment.
    /// </summary>
    /// <remarks>
    /// Required.
    /// </remarks>
    [JsonIgnore]
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the id of the associated item.
    /// </summary>
    /// <remarks>
    /// Required.
    /// </remarks>
    [JsonIgnore]
    public Guid ItemId { get; set; }

    /// <summary>
    ///     Gets or sets the start of the segment.
    /// </summary>
    /// <remarks>
    /// Required.
    /// </remarks>
    public int StartTick { get; set; }

    /// <summary>
    ///     Gets or sets the end of the segment.
    /// </summary>
    /// <remarks>
    /// Required.
    /// </remarks>
    public int EndTick { get; set; }

    /// <summary>
    ///     Gets or sets the Type of content this segment defines.
    /// </summary>
    /// <remarks>
    /// Required.
    /// </remarks>
    public MediaSegmentType Type { get; set; }
}
