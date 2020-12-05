using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.SyncPlay.Dtos
{
    /// <summary>
    /// Class GroupInfoDto.
    /// </summary>
    public class GroupInfoDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupInfoDto"/> class.
        /// </summary>
        /// <param name="groupId">The group identifier.</param>
        /// <param name="groupName">The group name.</param>
        /// <param name="visibility">The group visibility.</param>
        /// <param name="invitedUsers">The list of invited users.</param>
        /// <param name="administrators">The list of users that manage the group.</param>
        /// <param name="openPlaybackAccess">Whether new members will have playback access.</param>
        /// <param name="openPlaylistAccess">Whether new members will have playlist access.</param>
        /// <param name="state">The group state.</param>
        /// <param name="participants">The participants.</param>
        /// <param name="userNames">The list of user names.</param>
        /// <param name="accessList">The access list.</param>
        /// <param name="lastUpdatedAt">The date when this DTO has been created.</param>
        public GroupInfoDto(
            Guid groupId,
            string groupName,
            GroupVisibilityType visibility,
            IReadOnlyList<Guid> invitedUsers,
            IReadOnlyList<Guid> administrators,
            bool openPlaybackAccess,
            bool openPlaylistAccess,
            GroupStateType state,
            IReadOnlyList<Guid> participants,
            IReadOnlyList<string> userNames,
            IReadOnlyDictionary<Guid, UserGroupAccessDto> accessList,
            DateTime lastUpdatedAt)
        {
            GroupId = groupId;
            GroupName = groupName;
            Visibility = visibility;
            InvitedUsers = invitedUsers;
            Administrators = administrators;
            OpenPlaybackAccess = openPlaybackAccess;
            OpenPlaylistAccess = openPlaylistAccess;
            State = state;
            Participants = participants;
            UserNames = userNames;
            AccessList = accessList;
            LastUpdatedAt = lastUpdatedAt;
        }

        /// <summary>
        /// Gets the group identifier.
        /// </summary>
        /// <value>The group identifier.</value>
        public Guid GroupId { get; }

        /// <summary>
        /// Gets the group name.
        /// </summary>
        /// <value>The group name.</value>
        public string GroupName { get; }

        /// <summary>
        /// Gets the group visibility type.
        /// </summary>
        /// <value>The group visibility.</value>
        public GroupVisibilityType Visibility { get; }

        /// <summary>
        /// Gets the list of identifiers of the users that are invited to join.
        /// </summary>
        /// <value>The list of user identifiers.</value>
        public IReadOnlyList<Guid> InvitedUsers { get; }

        /// <summary>
        /// Gets the list of identifiers of the users that manage the group.
        /// </summary>
        /// <value>The list of user identifiers.</value>
        public IReadOnlyList<Guid> Administrators { get; }

        /// <summary>
        /// Gets a value indicating whether new members will have playback access.
        /// </summary>
        /// <value><c>true</c> if new members will have playback access; <c>false</c> otherwise.</value>
        public bool OpenPlaybackAccess { get; }

        /// <summary>
        /// Gets a value indicating whether new members will have playlist access.
        /// </summary>
        /// <value><c>true</c> if new members will have playlist access; <c>false</c> otherwise.</value>
        public bool OpenPlaylistAccess { get; }

        /// <summary>
        /// Gets the group state.
        /// </summary>
        /// <value>The group state.</value>
        public GroupStateType State { get; }

        /// <summary>
        /// Gets the participants.
        /// </summary>
        /// <value>The participants.</value>
        public IReadOnlyList<Guid> Participants { get; }

        /// <summary>
        /// Gets the participant's names.
        /// </summary>
        /// <value>The list of user names.</value>
        public IReadOnlyList<string> UserNames { get; }

        /// <summary>
        /// Gets the access list.
        /// </summary>
        /// <value>The access list.</value>
        public IReadOnlyDictionary<Guid, UserGroupAccessDto> AccessList { get; }

        /// <summary>
        /// Gets the date when this DTO has been created.
        /// </summary>
        /// <value>The date when this DTO has been created.</value>
        public DateTime LastUpdatedAt { get; }
    }
}
