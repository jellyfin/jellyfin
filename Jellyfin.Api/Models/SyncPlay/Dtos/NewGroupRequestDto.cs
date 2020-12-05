using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.SyncPlay.Dtos
{
    /// <summary>
    /// Class NewGroupRequestDto.
    /// </summary>
    public class NewGroupRequestDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewGroupRequestDto"/> class.
        /// </summary>
        public NewGroupRequestDto()
        {
            GroupName = string.Empty;
            InvitedUsers = Array.Empty<Guid>();
        }

        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        /// <value>The name of the new group.</value>
        public string GroupName { get; set; }

        /// <summary>
        /// Gets or sets the group visibility type.
        /// </summary>
        /// <value>The group visibility.</value>
        public GroupVisibilityType? Visibility { get; set; }

        /// <summary>
        /// Gets or sets the list of users that are invited to join the private group.
        /// </summary>
        /// <value>The list of user identifiers.</value>
        public IReadOnlyList<Guid> InvitedUsers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether new members will have playback access.
        /// </summary>
        /// <value><c>true</c> if new members will have playback access; <c>false</c> otherwise.</value>
        public bool? OpenPlaybackAccess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether new members will have playlist access.
        /// </summary>
        /// <value><c>true</c> if new members will have playlist access; <c>false</c> otherwise.</value>
        public bool? OpenPlaylistAccess { get; set; }
    }
}
