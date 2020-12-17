using MediaBrowser.Model.SyncPlay;

namespace MediaBrowser.Controller.SyncPlay.Requests
{
    /// <summary>
    /// Class NewGroupRequest.
    /// </summary>
    public class NewGroupRequest : ISyncPlayRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewGroupRequest"/> class.
        /// </summary>
        /// <param name="groupName">The name of the new group.</param>
        public NewGroupRequest(string groupName)
        {
            GroupName = groupName;
        }

        /// <summary>
        /// Gets the group name.
        /// </summary>
        /// <value>The name of the new group.</value>
        public string GroupName { get; }

        /// <inheritdoc />
        public RequestType Type { get; } = RequestType.NewGroup;
    }
}
