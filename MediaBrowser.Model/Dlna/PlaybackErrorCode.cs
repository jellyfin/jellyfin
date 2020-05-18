#pragma warning disable CS1591

namespace MediaBrowser.Model.Dlna
{
    public enum PlaybackErrorCode
    {
        /// <summary>
        /// Not allowed
        /// </summary>
        NotAllowed = 0,

        /// <summary>
        /// No compatible stream
        /// </summary>
        NoCompatibleStream = 1,

        /// <summary>
        /// Rate limit exceeded
        /// </summary>
        RateLimitExceeded = 2
    }
}
