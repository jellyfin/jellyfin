using System;

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class JoinGroupRequest.
    /// </summary>
    public class JoinGroupRequest
    {
        /// <summary>
        /// Gets or sets the group id.
        /// </summary>
        /// <value>The id of the group to join.</value>
        public Guid GroupId { get; set; }
    }
}
