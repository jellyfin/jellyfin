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
        public GroupInfoDto()
        {
            GroupId = string.Empty;
            GroupName = string.Empty;
            Participants = new List<string>();
        }

        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        /// <value>The group identifier.</value>
        public string GroupId { get; set; }

        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        /// <value>The group name.</value>
        public string GroupName { get; set; }

        /// <summary>
        /// Gets or sets the group state.
        /// </summary>
        /// <value>The group state.</value>
        public GroupStateType State { get; set; }

        /// <summary>
        /// Gets or sets the participants.
        /// </summary>
        /// <value>The participants.</value>
        public IReadOnlyList<string> Participants { get; set; }

        /// <summary>
        /// Gets or sets the date when this dto has been updated.
        /// </summary>
        /// <value>The date when this dto has been updated.</value>
        public DateTime LastUpdatedAt { get; set; }
    }
}
