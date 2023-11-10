#nullable disable

#pragma warning disable CA1002, CS1591

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Model.MediaSegments
{
    public interface IMediaSegmentsManager
    {
        /// <summary>
        /// Create or update a media segment.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <returns>New MediaSegment.</returns>
        Task<MediaSegment> CreateMediaSegmentAsync(MediaSegment segment);

        /// <summary>
        /// Create multiple new media segment.
        /// </summary>
        /// <param name="segments">List of segments.</param>
        /// <returns>New MediaSegment.</returns>
        Task<IEnumerable<MediaSegment>> CreateMediaSegmentsAsync(IEnumerable<MediaSegment> segments);

        /// <summary>
        /// Get all media segments.
        /// </summary>
        /// <param name="itemId">Optional: Just segments with MediaSourceId.</param>
        /// <param name="streamIndex">Optional: Just segments with MediaStreamIndex.</param>
        /// <param name="typeIndex">Optional: The typeIndex.</param>
        /// <param name="type">Optional: The segment type.</param>
        /// <returns>List of MediaSegment.</returns>
        List<MediaSegment> GetAllMediaSegments(Guid itemId = default, int streamIndex = -1, int typeIndex = -1, MediaSegmentType? type = null);

        /// <summary>
        /// Delete Media Segments.
        /// </summary>
        /// <param name="itemId">Required: The MediaSourceId.</param>
        /// <param name="streamIndex">Optional: Just segments with MediaStreamIndex.</param>
        /// <param name="typeIndex">Optional: The typeIndex.</param>
        /// <param name="type">Optional: The segment type.</param>
        /// <returns>Deleted segments.</returns>
        Task<List<MediaSegment>> DeleteSegmentsAsync(Guid itemId, int streamIndex = -1, int typeIndex = -1, MediaSegmentType? type = null);
    }
}
