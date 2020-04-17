namespace MediaBrowser.Model.Syncplay
{
    /// <summary>
    /// Enum GroupUpdateType
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
        /// The group-wait update. Tells members of the group that a user is buffering.
        /// </summary>
        GroupWait = 4,
        /// <summary>
        /// The prepare-session update. Tells a user to load some content.
        /// </summary>
        PrepareSession = 5,
        /// <summary>
        /// The not-in-group update. Tells a user that no group has been joined.
        /// </summary>
        NotInGroup = 7
    }
}
