using System;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data.Entities;

/// <summary>
///     An entity representing the metadata for a group of trickplay tiles.
/// </summary>
public class MediaSegment
{
    /// <summary>
    ///     Gets or sets the id of the media segment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    ///     Gets or sets the id of the associated item.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    ///     Gets or sets the Type of content this segment defines.
    /// </summary>
    public MediaSegmentType Type { get; set; }

    /// <summary>
    ///     Gets or sets the end of the segment.
    /// </summary>
    public long EndTicks { get; set; }

    /// <summary>
    ///     Gets or sets the start of the segment.
    /// </summary>
    public long StartTicks { get; set; }
}
