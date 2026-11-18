namespace MediaBrowser.Model.MediaInfo
{
    /// <summary>
    /// The protocol used to access a media source.
    /// </summary>
    public enum MediaProtocol
    {
        /// <summary>
        /// Local filesystem protocol.
        /// </summary>
        File = 0,

        /// <summary>
        /// Hypertext Transfer Protocol.
        /// </summary>
        Http = 1,

        /// <summary>
        /// Real-Time Messaging Protocol.
        /// </summary>
        Rtmp = 2,

        /// <summary>
        /// Real-Time Streaming Protocol.
        /// </summary>
        Rtsp = 3,

        /// <summary>
        /// User Datagram Protocol.
        /// </summary>
        Udp = 4,

        /// <summary>
        /// Real-time Transport Protocol.
        /// </summary>
        Rtp = 5,

        /// <summary>
        /// File Transfer Protocol.
        /// </summary>
        Ftp = 6
    }
}
