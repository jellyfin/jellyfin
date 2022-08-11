using System;
using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.Requests
{
    /// <summary>
    /// Class JoinGroupRequest.
    /// </summary>
    public class JoinGroupRequest : ISyncPlayRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JoinGroupRequest"/> class.
        /// </summary>
        /// <param name="groupId">The identifier of the group to join.</param>
        public JoinGroupRequest(Guid groupId)
        {
            GroupId = groupId;
        }

        /// <summary>
        /// Gets the group identifier.
        /// </summary>
        /// <value>The identifier of the group to join.</value>
        public Guid GroupId { get; }

        /// <inheritdoc />
        public RequestType Type { get; } = RequestType.JoinGroup;
    }
}
