using System;
using System.ComponentModel;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.MediaSegments;

/// <summary>
/// Api model for MediaSegment's.
/// </summary>
public class MediaSegmentDto
{
    /// <summary>
    /// Gets or sets the id of the media segment.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the id of the associated item.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the type of content this segment defines.
    /// </summary>
    [DefaultValue(MediaSegmentType.Unknown)]
    public MediaSegmentType Type { get; set; }

    /// <summary>
    /// Gets or sets the start of the segment.
    /// </summary>
    public long StartTicks { get; set; }

    /// <summary>
    /// Gets or sets the end of the segment.
    /// </summary>
    public long EndTicks { get; set; }
}
