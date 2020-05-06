namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Enum SyncPlayAccess.
    /// </summary>
    public enum SyncPlayAccess
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
        /// SyncPlay is disabled for the user.
        /// </summary>
        None
    }
}
