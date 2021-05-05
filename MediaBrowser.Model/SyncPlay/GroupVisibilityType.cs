namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Enum GroupVisibilityType.
    /// </summary>
    public enum GroupVisibilityType
    {
        /// <summary>
        /// Group is visible in the groups list and everyone can join.
        /// </summary>
        Public = 0,

        /// <summary>
        /// Group is not visible in the group list. Only invited users can join.
        /// </summary>
        InviteOnly = 1,

        /// <summary>
        /// Group is visible only to the user that created it.
        /// </summary>
        Private = 2
    }
}
