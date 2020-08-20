namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Enum PlaybackRequestType.
    /// </summary>
    public enum PlaybackRequestType
    {
        /// <summary>
        /// A user is requesting a play command for the group.
        /// </summary>
        Play = 0,

        /// <summary>
        /// A user is requesting a pause command for the group.
        /// </summary>
        Pause = 1,

        /// <summary>
        /// A user is requesting a seek command for the group.
        /// </summary>
        Seek = 2,

        /// <summary>
        /// A user is signaling that playback is buffering.
        /// </summary>
        Buffer = 3,

        /// <summary>
        /// A user is signaling that playback resumed.
        /// </summary>
        Ready = 4,

        /// <summary>
        /// A user is reporting its ping.
        /// </summary>
        Ping = 5
    }
}
