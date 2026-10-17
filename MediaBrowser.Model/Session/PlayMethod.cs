namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// The play method.
    /// </summary>
    public enum PlayMethod
    {
        /// <summary>
        /// The media is transcoded before it is sent to the client.
        /// </summary>
        Transcode = 0,

        /// <summary>
        /// The media is remuxed into a compatible container but the streams are not re-encoded.
        /// </summary>
        DirectStream = 1,

        /// <summary>
        /// The media is sent to the client as-is.
        /// </summary>
        DirectPlay = 2
    }
}
