namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Enum GroupQueueMode.
    /// </summary>
    public enum GroupQueueMode
    {
        /// <summary>
        /// Insert items at the end of the queue.
        /// </summary>
        Queue = 0,

        /// <summary>
        /// Insert items after the currently playing item.
        /// </summary>
        QueueNext = 1
    }
}
