namespace MediaBrowser.Model.MediaInfo
{
    /// <summary>
    /// The type of timestamps used in a transport stream.
    /// </summary>
    public enum TransportStreamTimestamp
    {
        /// <summary>
        /// The stream contains no timestamps.
        /// </summary>
        None,

        /// <summary>
        /// The stream contains zero-value timestamps.
        /// </summary>
        Zero,

        /// <summary>
        /// The stream contains valid timestamps.
        /// </summary>
        Valid
    }
}
