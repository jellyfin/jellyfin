using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.SyncPlayDtos
{
    /// <summary>
    /// Class RemoveFromPlaylistRequestDto.
    /// </summary>
    public class RemoveFromPlaylistRequestDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveFromPlaylistRequestDto"/> class.
        /// </summary>
        public RemoveFromPlaylistRequestDto()
        {
            PlaylistItemIds = Array.Empty<Guid>();
        }

        /// <summary>
        /// Gets or sets the playlist identifiers ot the items.
        /// </summary>
        /// <value>The playlist identifiers ot the items.</value>
        public IReadOnlyList<Guid> PlaylistItemIds { get; set; }
    }
}
