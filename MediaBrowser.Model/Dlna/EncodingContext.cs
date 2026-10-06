namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// The encoding context.
    /// </summary>
    public enum EncodingContext
    {
        /// <summary>
        /// The media is transcoded on the fly and delivered as a stream.
        /// </summary>
        Streaming = 0,

        /// <summary>
        /// The media is transcoded to a static file.
        /// </summary>
        Static = 1
    }
}
