using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.MediaSegments;

namespace MediaBrowser.Controller;

/// <summary>
/// Defines methods for interacting with media segments.
/// </summary>
public interface IMediaSegmentManager
{
    /// <summary>
    /// Returns if this item supports media segments.
    /// </summary>
    /// <param name="baseItem">The base Item to check.</param>
    /// <returns>True if supported otherwise false.</returns>
    bool IsTypeSupported(BaseItem baseItem);

    /// <summary>
    /// Creates a new Media Segment associated with an Item.
    /// </summary>
    /// <param name="mediaSegment">The segment to create.</param>
    /// <param name="segmentProviderId">The id of the Provider who created this segment.</param>
    /// <returns>The created Segment entity.</returns>
    Task<MediaSegmentDto> CreateSegmentAsync(MediaSegmentDto mediaSegment, string segmentProviderId);

    /// <summary>
    /// Deletes a single media segment.
    /// </summary>
    /// <param name="segmentId">The <see cref="MediaSegment.Id"/> to delete.</param>
    /// <returns>a task.</returns>
    Task DeleteSegmentAsync(Guid segmentId);

    /// <summary>
    /// Obtains all segments accociated with the itemId.
    /// </summary>
    /// <param name="itemId">The id of the <see cref="BaseItem"/>.</param>
    /// <param name="typeFilter">filteres all media segments of the given type to be included. If null all types are included.</param>
    /// <returns>An enumerator of <see cref="MediaSegmentDto"/>'s.</returns>
    Task<IEnumerable<MediaSegmentDto>> GetSegmentsAsync(Guid itemId, IEnumerable<MediaSegmentType>? typeFilter);

    /// <summary>
    /// Gets information about any media segments stored for the given itemId.
    /// </summary>
    /// <param name="itemId">The id of the <see cref="BaseItem"/>.</param>
    /// <returns>True if there are any segments stored for the item, otherwise false.</returns>
    /// TODO: this should be async but as the only caller BaseItem.GetVersionInfo isn't async, this is also not. Venson.
    bool HasSegments(Guid itemId);
}
