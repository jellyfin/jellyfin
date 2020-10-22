namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Enum GroupRequestType.
    /// </summary>
    public enum GroupRequestType
    {
        /// <summary>
        /// A user is requesting to create a new group.
        /// </summary>
        NewGroup,

        /// <summary>
        /// A user is requesting to join a group.
        /// </summary>
        JoinGroup,

        /// <summary>
        /// A user is requesting to leave a group.
        /// </summary>
        LeaveGroup,

        /// <summary>
        /// A user is requesting the list of available groups.
        /// </summary>
        ListGroups,

        /// <summary>
        /// A user is sending a playback command to a group.
        /// </summary>
        Playback
    }
}
