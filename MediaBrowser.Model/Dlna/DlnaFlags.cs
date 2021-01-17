using System;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the DlnaFlags.
    /// </summary>
    [Flags]
#pragma warning disable CA1028 // Enum Storage should be Int32
    public enum DlnaFlags : ulong
#pragma warning restore CA1028 // Enum Storage should be Int32
    {
        /// <summary>
        /// <i>Background</i> transfer mode.
        /// For use with upload and download transfers to and from the server.
        /// The primary difference between \ref DH_TransferMode_Interactive and \ref DH_TransferMode_Bulk is that the latter assumes
        /// that the user is not relying on the transfer for immediately rendering the content and there are no issues with causing
        /// a buffer overflow if the receiver uses TCP flow control to reduce total throughput.
        /// </summary>
        BackgroundTransferMode = 1 << 22,

        /// <summary>
        /// Defines the ByteBasedSeek.
        /// </summary>
        ByteBasedSeek = 1 << 29,

        /// <summary>
        /// Defines the ConnectionStall.
        /// </summary>
        ConnectionStall = 1 << 21,

        /// <summary>
        /// Defines the DlnaV15.
        /// </summary>
        DlnaV15 = 1 << 20,

        /// <summary>
        /// <i>Interactive</i> transfer mode.
        /// For best effort transfer of images and non-real-time transfers. URIs with image content usually support
        /// \ref DH_TransferMode_Bulk too.
        /// The primary difference between \ref DH_TransferMode_Interactive and \ref DH_TransferMode_Bulk is that the
        /// former assumes that the transfer is intended for immediate rendering.
        /// </summary>
        InteractiveTransferMode = 1 << 23,

        /// <summary>
        /// Defines the PlayContainer.
        /// </summary>
        PlayContainer = 1 << 28,

        /// <summary>
        /// Defines the RtspPause.
        /// </summary>
        RtspPause = 1 << 25,

        /// <summary>
        /// Defines the S0Increase.
        /// </summary>
        S0Increase = 1 << 27,

        /// <summary>
        /// Defines the SenderPaced.
        /// </summary>
        SenderPaced = 1L << 31,

        /// <summary>
        /// Defines the SnIncrease.
        /// </summary>
        SnIncrease = 1 << 26,

        /// <summary>
        /// <i>Streaming</i> transfer mode.
        /// The server transmits at a throughput sufficient for real-time playback of audio or video. URIs with audio or video
        /// often support the \ref DH_TransferMode_Interactive and \ref DH_TransferMode_Bulk transfer modes. The most well-known
        /// exception to this general claim is for live streams.
        /// </summary>
        StreamingTransferMode = 1 << 24,

        /// <summary>
        /// Defines the TimeBasedSeek.
        /// </summary>
        TimeBasedSeek = 1 << 30
    }
}
