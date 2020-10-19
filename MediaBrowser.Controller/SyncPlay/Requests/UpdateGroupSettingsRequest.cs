using System;
using System.Collections.Generic;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.Requests
{
    /// <summary>
    /// Class UpdateGroupSettingsRequest.
    /// </summary>
    public class UpdateGroupSettingsRequest : ISyncPlayRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateGroupSettingsRequest"/> class.
        /// </summary>
        /// <param name="groupName">The name of the new group.</param>
        /// <param name="visibility">The group visibility.</param>
        /// <param name="invitedUsers">The list of invited user.</param>
        /// <param name="openPlaybackAccess">Whether new members will have playback access.</param>
        /// <param name="openPlaylistAccess">Whether new members will have playlist access.</param>
        /// <param name="accessListUserIds">The list of users whose permissions are changing.</param>
        /// <param name="accessListPlayback">The list of new playback permissions.</param>
        /// <param name="accessListPlaylist">The list of new playlist permissions.</param>
        public UpdateGroupSettingsRequest(
            string groupName,
            GroupVisibilityType? visibility,
            IReadOnlyList<Guid> invitedUsers,
            bool? openPlaybackAccess,
            bool? openPlaylistAccess,
            IReadOnlyList<Guid> accessListUserIds,
            IReadOnlyList<bool> accessListPlayback,
            IReadOnlyList<bool> accessListPlaylist)
        {
            GroupName = groupName;
            Visibility = visibility;
            InvitedUsers = invitedUsers;
            OpenPlaybackAccess = openPlaybackAccess;
            OpenPlaylistAccess = openPlaylistAccess;
            AccessListUserIds = accessListUserIds;
            AccessListPlayback = accessListPlayback;
            AccessListPlaylist = accessListPlaylist;
        }

        /// <summary>
        /// Gets the group name.
        /// </summary>
        /// <value>The name of the new group.</value>
        public string GroupName { get; }

        /// <summary>
        /// Gets the group visibility type.
        /// </summary>
        /// <value>The group visibility.</value>
        public GroupVisibilityType? Visibility { get; }

        /// <summary>
        /// Gets the list of users that are invited to join the private group.
        /// </summary>
        /// <value>The list of user identifiers.</value>
        public IReadOnlyList<Guid> InvitedUsers { get; }

        /// <summary>
        /// Gets a value indicating whether new members will have playback access.
        /// </summary>
        /// <value><c>true</c> if new members will have playback access; <c>false</c> otherwise.</value>
        public bool? OpenPlaybackAccess { get; }

        /// <summary>
        /// Gets a value indicating whether new members will have playlist access.
        /// </summary>
        /// <value><c>true</c> if new members will have playlist access; <c>false</c> otherwise.</value>
        public bool? OpenPlaylistAccess { get; }

        /// <summary>
        /// Gets the list of users whose permissions are changing.
        /// </summary>
        /// <value>The list of user identifiers.</value>
        public IReadOnlyList<Guid> AccessListUserIds { get; }

        /// <summary>
        /// Gets the list of new playback permissions.
        /// </summary>
        /// <value>The list of new playback permissions.</value>
        public IReadOnlyList<bool> AccessListPlayback { get; }

        /// <summary>
        /// Gets the list of new playlist permissions.
        /// </summary>
        /// <value>The list of new playlist permissions.</value>
        public IReadOnlyList<bool> AccessListPlaylist { get; }

        /// <inheritdoc />
        public RequestType Type { get; } = RequestType.UpdateGroupSettings;
    }
}
