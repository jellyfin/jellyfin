#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Dlna
{
    [Flags]
    public enum DlnaFlags : ulong
    {
        /*! <i>Background</i> transfer mode.
            For use with upload and download transfers to and from the server.
            The primary difference between \ref DH_TransferMode_Interactive and
            \ref DH_TransferMode_Bulk is that the latter assumes that the user
            is not relying on the transfer for immediately rendering the content
            and there are no issues with causing a buffer overflow if the
            receiver uses TCP flow control to reduce total throughput.
        */
        BackgroundTransferMode = 1 << 22,

        ByteBasedSeek = 1 << 29,
        ConnectionStall = 1 << 21,

        DlnaV15 = 1 << 20,

        /*! <i>Interactive</i> transfer mode.
            For best effort transfer of images and non-real-time transfers.
            URIs with image content usually support \ref DH_TransferMode_Bulk too.
            The primary difference between \ref DH_TransferMode_Interactive and
            \ref DH_TransferMode_Bulk is that the former assumes that the
            transfer is intended for immediate rendering.
        */
        InteractiveTransferMode = 1 << 23,

        PlayContainer = 1 << 28,
        RtspPause = 1 << 25,
        S0Increase = 1 << 27,
        SenderPaced = 1L << 31,
        SnIncrease = 1 << 26,

        /*! <i>Streaming</i> transfer mode.
            The server transmits at a throughput sufficient for real-time playback of
            audio or video. URIs with audio or video often support the
            \ref DH_TransferMode_Interactive and \ref DH_TransferMode_Bulk transfer modes.
            The most well-known exception to this general claim is for live streams.
        */
        StreamingTransferMode = 1 << 24,

        TimeBasedSeek = 1 << 30
    }
}
