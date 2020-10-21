using System;

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class JoinGroupRequest.
    /// </summary>
    public class JoinGroupRequest
    {
        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        /// <value>The identifier of the group to join.</value>
        public Guid GroupId { get; set; }
    }
}
