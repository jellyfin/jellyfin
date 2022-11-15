namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Enum SendCommandType.
    /// </summary>
    public enum SendCommandType
    {
        /// <summary>
        /// The unpause command. Instructs users to unpause playback.
        /// </summary>
        Unpause = 0,

        /// <summary>
        /// The pause command. Instructs users to pause playback.
        /// </summary>
        Pause = 1,

        /// <summary>
        /// The stop command. Instructs users to stop playback.
        /// </summary>
        Stop = 2,

        /// <summary>
        /// The seek command. Instructs users to seek to a specified time.
        /// </summary>
        Seek = 3
    }
}
