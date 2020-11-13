namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class GroupStateUpdate.
    /// </summary>
    public class GroupStateUpdate
    {
        /// <summary>
        /// Gets or sets the state of the group.
        /// </summary>
        /// <value>The state of the group.</value>
        public GroupStateType State { get; set; }

        /// <summary>
        /// Gets or sets the reason of the state change.
        /// </summary>
        /// <value>The reason of the state change.</value>
        public PlaybackRequestType Reason { get; set; }
    }
}
