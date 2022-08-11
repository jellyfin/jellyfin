namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Used to filter the sessions of a group.
    /// </summary>
    public enum SyncPlayBroadcastType
    {
        /// <summary>
        /// All sessions will receive the message.
        /// </summary>
        AllGroup = 0,

        /// <summary>
        /// Only the specified session will receive the message.
        /// </summary>
        CurrentSession = 1,

        /// <summary>
        /// All sessions, except the current one, will receive the message.
        /// </summary>
        AllExceptCurrentSession = 2,

        /// <summary>
        /// Only sessions that are not buffering will receive the message.
        /// </summary>
        AllReady = 3
    }
}
