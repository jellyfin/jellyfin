namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// The repeat mode of a play queue.
    /// </summary>
    public enum RepeatMode
    {
        /// <summary>
        /// Nothing is repeated.
        /// </summary>
        RepeatNone = 0,

        /// <summary>
        /// The whole queue is repeated.
        /// </summary>
        RepeatAll = 1,

        /// <summary>
        /// The current item is repeated.
        /// </summary>
        RepeatOne = 2
    }
}
