namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// The status of a live TV service.
    /// </summary>
    public enum LiveTvServiceStatus
    {
        /// <summary>
        /// The service is available.
        /// </summary>
        Ok = 0,

        /// <summary>
        /// The service is unavailable.
        /// </summary>
        Unavailable = 1
    }
}
