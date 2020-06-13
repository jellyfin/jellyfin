namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Enum SendCommandType.
    /// </summary>
    public enum SendCommandType
    {
        /// <summary>
        /// The play command. Instructs users to start playback.
        /// </summary>
        Play = 0,
        /// <summary>
        /// The pause command. Instructs users to pause playback.
        /// </summary>
        Pause = 1,
        /// <summary>
        /// The seek command. Instructs users to seek to a specified time.
        /// </summary>
        Seek = 2
    }
}
