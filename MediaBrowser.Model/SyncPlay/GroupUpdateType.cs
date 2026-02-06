namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Enum GroupUpdateType.
    /// </summary>
    public enum GroupUpdateType
    {
        /// <summary>
        /// The user-joined update. Tells members of a group about a new user.
        /// </summary>
        UserJoined = 0,

        /// <summary>
        /// The user-left update. Tells members of a group that a user left.
        /// </summary>
        UserLeft = 1,

        /// <summary>
        /// The group-joined update. Tells a user that the group has been joined.
        /// </summary>
        GroupJoined = 2,

        /// <summary>
        /// The group-left update. Tells a user that the group has been left.
        /// </summary>
        GroupLeft = 3,

        /// <summary>
        /// The group-update. Updates information about the group.
        /// </summary>
        GroupUpdate = 4,

        /// <summary>
        /// The play-queue update. Tells a user the playing queue of the group.
        /// </summary>
        PlayQueue = 5,

        /// <summary>
        /// The group-state update. Tells members of the group that the state changed.
        /// </summary>
        StateUpdate = 6,

        /// <summary>
        /// The not-in-group error. Tells a user that they don't belong to a group.
        /// </summary>
        NotInGroup = 7,

        /// <summary>
        /// The group-does-not-exist error. Sent when trying to join a non-existing group.
        /// </summary>
        GroupDoesNotExist = 8,

        /// <summary>
        /// The create-group-denied error. Sent when a user isn't allowed to create groups.
        /// </summary>
        CreateGroupDenied = 9,

        /// <summary>
        /// The join-group-denied error. Sent when a user isn't allowed to join groups.
        /// </summary>
        JoinGroupDenied = 10,

        /// <summary>
        /// The library-access-denied error. Sent when a user tries to join a group without required access to the library.
        /// </summary>
        LibraryAccessDenied = 11,

        /// <summary>
        /// The group-snapshot update. Sends a full group snapshot to a client.
        /// </summary>
        GroupSnapshot = 12
    }
}
