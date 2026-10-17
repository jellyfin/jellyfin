namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// The transcode seek info.
    /// </summary>
    public enum TranscodeSeekInfo
    {
        /// <summary>
        /// The seek method is chosen automatically.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Seeking is performed by byte position.
        /// </summary>
        Bytes = 1
    }
}
