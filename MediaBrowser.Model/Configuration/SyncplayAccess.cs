namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Enum SyncplayAccess.
    /// </summary>
    public enum SyncplayAccess
    {
        /// <summary>
        /// User can create groups and join them.
        /// </summary>
        CreateAndJoinGroups,

        /// <summary>
        /// User can only join already existing groups.
        /// </summary>
        JoinGroups,

        /// <summary>
        /// Syncplay is disabled for the user.
        /// </summary>
        None
    }
}
