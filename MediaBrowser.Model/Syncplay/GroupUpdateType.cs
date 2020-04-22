namespace MediaBrowser.Model.Syncplay
{
    /// <summary>
    /// Enum GroupUpdateType.
    /// </summary>
    public enum GroupUpdateType
    {
        /// <summary>
        /// The user-joined update. Tells members of a group about a new user.
        /// </summary>
        UserJoined,
        /// <summary>
        /// The user-left update. Tells members of a group that a user left.
        /// </summary>
        UserLeft,
        /// <summary>
        /// The group-joined update. Tells a user that the group has been joined.
        /// </summary>
        GroupJoined,
        /// <summary>
        /// The group-left update. Tells a user that the group has been left.
        /// </summary>
        GroupLeft,
        /// <summary>
        /// The group-wait update. Tells members of the group that a user is buffering.
        /// </summary>
        GroupWait,
        /// <summary>
        /// The prepare-session update. Tells a user to load some content.
        /// </summary>
        PrepareSession,
        /// <summary>
        /// The not-in-group error. Tells a user that it doesn't belong to a group.
        /// </summary>
        NotInGroup,
        /// <summary>
        /// The group-not-joined error. Sent when a request to join a group fails.
        /// </summary>
        GroupNotJoined,
        /// <summary>
        /// The create-group-denied error. Sent when a user tries to create a group without required permissions.
        /// </summary>
        CreateGroupDenied,
        /// <summary>
        /// The join-group-denied error. Sent when a user tries to join a group without required permissions.
        /// </summary>
        JoinGroupDenied,
        /// <summary>
        /// The library-access-denied error. Sent when a user tries to join a group without required access to the library.
        /// </summary>
        LibraryAccessDenied
    }
}
