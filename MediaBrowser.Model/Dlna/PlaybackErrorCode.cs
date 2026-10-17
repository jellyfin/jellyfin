namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// The playback error code.
    /// </summary>
    public enum PlaybackErrorCode
    {
        /// <summary>
        /// Playback of the item is not allowed.
        /// </summary>
        NotAllowed = 0,

        /// <summary>
        /// No stream compatible with the device profile was found.
        /// </summary>
        NoCompatibleStream = 1,

        /// <summary>
        /// The rate limit has been exceeded.
        /// </summary>
        RateLimitExceeded = 2
    }
}
