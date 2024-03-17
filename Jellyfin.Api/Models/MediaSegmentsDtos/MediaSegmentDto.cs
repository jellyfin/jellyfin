using System;
using Jellyfin.Data.Entities.MediaSegment;
using Jellyfin.Data.Enums.MediaSegmentAction;
using Jellyfin.Data.Enums.MediaSegmentType;

namespace Jellyfin.Api.Models.MediaSegmentsDtos;

/// <summary>
/// Media Segment dto.
/// </summary>
public class MediaSegmentDto
{
    /// <summary>
    /// Gets or sets the start position in Ticks.
    /// </summary>
    /// <value>The start position.</value>
    public long StartTicks { get; set; }

    /// <summary>
    /// Gets or sets the end position in Ticks.
    /// </summary>
    /// <value>The end position.</value>
    public long EndTicks { get; set; }

    /// <summary>
    /// Gets or sets the Type.
    /// </summary>
    /// <value>The media segment type.</value>
    public MediaSegmentType Type { get; set; }

    /// <summary>
    /// Gets or sets the TypeIndex which allows to identify multiple segments that are of the same type and streamindex.
    /// </summary>
    /// <value>The type index.</value>
    public int TypeIndex { get; set; }

    /// <summary>
    /// Gets or sets the associated MediaSourceId.
    /// </summary>
    /// <value>The id.</value>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the associated MediaStreamIndex.
    /// </summary>
    /// <value>The id.</value>
    public int StreamIndex { get; set; }

    /// <summary>
    /// Gets or sets the creator recommended action. Can be overwritten with user defined action.
    /// </summary>
    /// <value>The media segment action.</value>
    public MediaSegmentAction Action { get; set; }

    /// <summary>
    /// Gets or sets a comment.
    /// </summary>
    /// <value>The user provided value to be displayed when the <see cref="MediaSegmentDto.Type"/> is a <see cref="MediaSegmentType.Annotation" />.</value>
    public string? Comment { get; set; }

    /// <summary>
    /// Convert the dto to the <see cref="MediaSegment"/> model.
    /// </summary>
    /// <returns>The converted <see cref="MediaSegment"/> model.</returns>
    public MediaSegment ToMediaSegment()
    {
        return new MediaSegment
        {
            StartTicks = StartTicks,
            EndTicks = EndTicks,
            Type = Type,
            TypeIndex = TypeIndex,
            ItemId = ItemId,
            StreamIndex = StreamIndex,
            Action = Action,
            Comment = Comment
        };
    }

    /// <summary>
    /// Convert the <see cref="MediaSegment"/> to dto model.
    /// </summary>
    /// <param name="seg">segment to convert.</param>
    /// <returns>The converted <see cref="MediaSegmentDto"/> model.</returns>
    public static MediaSegmentDto FromMediaSegment(MediaSegment seg)
    {
        return new MediaSegmentDto
        {
            StartTicks = seg.StartTicks,
            EndTicks = seg.EndTicks,
            Type = seg.Type,
            TypeIndex = seg.TypeIndex,
            ItemId = seg.ItemId,
            StreamIndex = seg.StreamIndex,
            Action = seg.Action,
            Comment = seg.Comment
        };
    }
}
