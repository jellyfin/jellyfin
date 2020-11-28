using System;

namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class JoinGroupRequestBody.
    /// </summary>
    public class JoinGroupRequestBody
    {
        /// <summary>
        /// Gets or sets the group identifier.
        /// </summary>
        /// <value>The identifier of the group to join.</value>
        public Guid GroupId { get; set; }
    }
}
