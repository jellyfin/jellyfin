using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller;

/// <summary>
///     Defines methods for interacting with media segments.
/// </summary>
public interface IMediaSegmentManager
{
    /// <summary>
    ///     Creates a new Media Segment associated with an Item.
    /// </summary>
    /// <param name="mediaSegment">The segment to create.</param>
    /// <returns>The created Segment entity</returns>
    Task<MediaSegment> CreateSegmentAsync(MediaSegment mediaSegment);

    /// <summary>
    ///     Deletes a single media segment.
    /// </summary>
    /// <param name="segmentId">The <see cref="MediaSegment.Id"/> to delete.</param>
    /// <returns>a task.</returns>
    Task DeleteSegmentAsync(Guid segmentId);

    /// <summary>
    ///     Obtains all segments accociated with the itemId.
    /// </summary>
    /// <param name="itemId">The id of the <see cref="BaseItem"/>.</param>
    /// <returns>An enumerator of <see cref="MediaSegment"/>'s.</returns>
    IAsyncEnumerable<MediaSegment> GetSegmentsAsync(Guid itemId);
}
