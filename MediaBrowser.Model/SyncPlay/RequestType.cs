namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Enum RequestType.
    /// </summary>
    public enum RequestType
    {
        /// <summary>
        /// A user is requesting to create a new group.
        /// </summary>
        NewGroup = 0,

        /// <summary>
        /// A user is requesting to join a group.
        /// </summary>
        JoinGroup = 1,

        /// <summary>
        /// A user is requesting to leave a group.
        /// </summary>
        LeaveGroup = 2,

        /// <summary>
        /// A user is requesting the list of available groups.
        /// </summary>
        ListGroups = 3,

        /// <summary>
        /// A user is sending a playback command to a group.
        /// </summary>
        Playback = 4
    }
}
