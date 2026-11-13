using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.SyncPlay
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
        /// <param name="state">The group state.</param>
        /// <param name="participants">The participants.</param>
        /// <param name="lastUpdatedAt">The date when this DTO has been created.</param>
        /// <param name="hostUsername">The username of the group's host.</param>
        public GroupInfoDto(Guid groupId, string groupName, GroupStateType state, IReadOnlyList<string> participants, DateTime lastUpdatedAt, string? hostUsername = null)
        {
            GroupId = groupId;
            GroupName = groupName;
            State = state;
            Participants = participants;
            LastUpdatedAt = lastUpdatedAt;
            HostUsername = hostUsername ?? string.Empty;
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
        /// Gets the group state.
        /// </summary>
        /// <value>The group state.</value>
        public GroupStateType State { get; }

        /// <summary>
        /// Gets the participants.
        /// </summary>
        /// <value>The participants.</value>
        public IReadOnlyList<string> Participants { get; }

        /// <summary>
        /// Gets the date when this DTO has been created.
        /// </summary>
        /// <value>The date when this DTO has been created.</value>
        public DateTime LastUpdatedAt { get; }

        /// <summary>
        /// Gets the username of the group's host. Initially the user that created the group;
        /// if the host leaves while other participants remain, the server promotes the
        /// longest-standing remaining participant. This always identifies a current member
        /// of the group, so clients can use it as a stable "who is host" identity instead of
        /// deriving it from participant list ordering.
        /// </summary>
        /// <value>The host's username.</value>
        public string HostUsername { get; }
    }
}
