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
        Task<MediaSegment> CreateMediaSegment(MediaSegment segment);

        /// <summary>
        /// Create or update multiple media segments.
        /// </summary>
        /// <param name="segments">List of segments.</param>
        /// <returns>New or updated MediaSegments.</returns>
        Task<IReadOnlyList<MediaSegment>> CreateMediaSegments(IReadOnlyList<MediaSegment> segments);

        /// <summary>
        /// Get all media segments.
        /// </summary>
        /// <param name="itemId">Optional: Just segments with itemId.</param>
        /// <param name="streamIndex">Optional: Just segments with MediaStreamIndex.</param>
        /// <param name="typeIndex">Optional: The typeIndex.</param>
        /// <param name="type">Optional: The segment type.</param>
        /// <returns>List of MediaSegment.</returns>
        public Task<List<MediaSegment>> GetAllMediaSegments(Guid itemId, int? streamIndex = null, int? typeIndex = null, MediaSegmentType? type = null);

        /// <summary>
        /// Delete Media Segments.
        /// </summary>
        /// <param name="itemId">Required: The itemId.</param>
        /// <param name="streamIndex">Optional: Just segments with MediaStreamIndex.</param>
        /// <param name="typeIndex">Optional: The typeIndex.</param>
        /// <param name="type">Optional: The segment type.</param>
        /// <returns>Deleted segments.</returns>
        Task<List<MediaSegment>> DeleteSegments(Guid itemId, int? streamIndex = null, int? typeIndex = null, MediaSegmentType? type = null);
    }
}
