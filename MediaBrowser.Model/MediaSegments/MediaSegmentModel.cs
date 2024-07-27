using System;

namespace MediaBrowser.Model.MediaSegments;

/// <summary>
///     Api model for MediaSegment's.
/// </summary>
public class MediaSegmentModel
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
    ///     Gets or sets the start of the segment.
    /// </summary>
    public int StartTick { get; set; }

    /// <summary>
    ///     Gets or sets the end of the segment.
    /// </summary>
    public int EndTick { get; set; }

    /// <summary>
    ///     Gets or sets the Type of content this segment defines.
    /// </summary>
    public MediaSegmentTypeModel Type { get; set; }
}
