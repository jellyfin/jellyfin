namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Enum GroupState.
    /// </summary>
    public enum GroupStateType
    {
        /// <summary>
        /// The group is in idle state. No media is playing.
        /// </summary>
        Idle = 0,

        /// <summary>
        /// The group is in waiting state. Playback is paused. Will start playing when users are ready.
        /// </summary>
        Waiting = 1,

        /// <summary>
        /// The group is in paused state. Playback is paused. Will resume on play command.
        /// </summary>
        Paused = 2,

        /// <summary>
        /// The group is in playing state. Playback is advancing.
        /// </summary>
        Playing = 3
    }
}
