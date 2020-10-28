using System;

namespace Jellyfin.Api.Models.SyncPlayDtos
{
    /// <summary>
    /// Class JoinGroupRequestDto.
    /// </summary>
    public class JoinGroupRequestDto
    {
        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        /// <value>The identifier of the group to join.</value>
        public Guid GroupId { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the remote session that will join the group instead.
        /// </summary>
        /// <value>The identifier of the remote session.</value>
        public string? RemoteSessionId { get; set; }
    }
}
