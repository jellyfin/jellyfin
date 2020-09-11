using System;

namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class JoinGroupRequest.
    /// </summary>
    public class JoinGroupRequest
    {
        /// <summary>
        /// Gets or sets the Group id.
        /// </summary>
        /// <value>The Group id to join.</value>
        public Guid GroupId { get; set; }
    }
}
