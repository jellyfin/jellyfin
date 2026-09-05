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
        /// <param name="bufferingParticipants">The usernames of participants currently buffering and blocking the group.</param>
        public GroupInfoDto(Guid groupId, string groupName, GroupStateType state, IReadOnlyList<string> participants, DateTime lastUpdatedAt, IReadOnlyList<string>? bufferingParticipants = null)
        {
            GroupId = groupId;
            GroupName = groupName;
            State = state;
            Participants = participants;
            LastUpdatedAt = lastUpdatedAt;
            BufferingParticipants = bufferingParticipants ?? Array.Empty<string>();
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
        /// Gets the usernames of the participants that are currently buffering and blocking the group.
        /// </summary>
        /// <value>The usernames of the participants currently buffering.</value>
        public IReadOnlyList<string> BufferingParticipants { get; }
    }
}
