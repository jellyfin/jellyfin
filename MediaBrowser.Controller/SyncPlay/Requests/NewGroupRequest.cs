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
        /// <param name="startingPlaybackRate">The starting playback rate.</param>
        public NewGroupRequest(string groupName, float startingPlaybackRate)
        {
            GroupName = groupName;
            StartingPlaybackRate = startingPlaybackRate;
        }

        /// <summary>
        /// Gets the group name.
        /// </summary>
        /// <value>The name of the new group.</value>
        public string GroupName { get; }

        /// <summary>
        /// Gets the starting playback rate.
        /// </summary>
        /// <value>The starting playback rate.</value>
        public float StartingPlaybackRate { get; }

        /// <inheritdoc />
        public RequestType Type { get; } = RequestType.NewGroup;
    }
}
