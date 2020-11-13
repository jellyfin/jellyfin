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
        Idle,

        /// <summary>
        /// The group is in wating state. Playback is paused. Will start playing when users are ready.
        /// </summary>
        Waiting,

        /// <summary>
        /// The group is in paused state. Playback is paused. Will resume on play command.
        /// </summary>
        Paused,

        /// <summary>
        /// The group is in playing state. Playback is advancing.
        /// </summary>
        Playing
    }
}
