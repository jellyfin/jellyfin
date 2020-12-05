namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class GroupStateUpdateDto.
    /// </summary>
    public class GroupStateUpdateDto
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupStateUpdateDto"/> class.
        /// </summary>
        /// <param name="state">The state of the group.</param>
        /// <param name="reason">The reason of the state change.</param>
        public GroupStateUpdateDto(GroupStateType state, PlaybackRequestType reason)
        {
            State = state;
            Reason = reason;
        }

        /// <summary>
        /// Gets the state of the group.
        /// </summary>
        /// <value>The state of the group.</value>
        public GroupStateType State { get; }

        /// <summary>
        /// Gets the reason of the state change.
        /// </summary>
        /// <value>The reason of the state change.</value>
        public PlaybackRequestType Reason { get; }
    }
}
