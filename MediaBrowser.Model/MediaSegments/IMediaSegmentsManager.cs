using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Data.Entities.MediaSegment;
using Jellyfin.Data.Enums.MediaSegmentType;
using Jellyfin.Data.Events;

namespace MediaBrowser.Model.MediaSegments;

/// <summary>
/// Media segments manager definition.
/// </summary>
public interface IMediaSegmentsManager
{
    /// <summary>
    /// Occurs when new or updated segments are available for itemId.
    /// </summary>
    public event EventHandler<GenericEventArgs<Guid>>? SegmentsAddedOrUpdated;

    /// <summary>
    /// Create or update multiple media segments.
    /// </summary>
    /// <param name="itemId">The item to create segments for.</param>
    /// <param name="segments">List of segments.</param>
    /// <returns>New or updated MediaSegments.</returns>
    /// <exception cref="InvalidOperationException">Will be thrown when an non existing item is requested.</exception>
    Task<IReadOnlyList<MediaSegment>> CreateMediaSegments(Guid itemId, IReadOnlyList<MediaSegment> segments);

    /// <summary>
    /// Get all media segments.
    /// </summary>
    /// <param name="itemId">Optional: Just segments with itemId.</param>
    /// <param name="streamIndex">Optional: Just segments with MediaStreamIndex.</param>
    /// <param name="typeIndex">Optional: The typeIndex.</param>
    /// <param name="type">Optional: The segment type.</param>
    /// <returns>List of MediaSegment.</returns>
    /// <exception cref="InvalidOperationException">Will be thrown when an non existing item is requested.</exception>
    public Task<IReadOnlyList<MediaSegment>> GetAllMediaSegments(Guid itemId, int? streamIndex = null, int? typeIndex = null, MediaSegmentType? type = null);

    /// <summary>
    /// Delete Media Segments.
    /// </summary>
    /// <param name="itemId">Required: The itemId.</param>
    /// <param name="streamIndex">Optional: Just segments with MediaStreamIndex.</param>
    /// <param name="typeIndex">Optional: The typeIndex.</param>
    /// <param name="type">Optional: The segment type.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Will be thrown when a empty Guid is requested.</exception>
    public Task DeleteSegments(Guid itemId, int? streamIndex = null, int? typeIndex = null, MediaSegmentType? type = null);
}
