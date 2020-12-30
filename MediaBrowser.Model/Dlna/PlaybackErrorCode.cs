namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the PlaybackErrorCode.
    /// </summary>
    public enum PlaybackErrorCode
    {
        /// <summary>
        /// Defines the error code NotAllowed.
        /// </summary>
        NotAllowed = 0,

        /// <summary>
        /// Defines the error code NoCompatibleStream.
        /// </summary>
        NoCompatibleStream = 1,

        /// <summary>
        /// Defines the error code RateLimitExceeded.
        /// </summary>
        RateLimitExceeded = 2
    }
}
