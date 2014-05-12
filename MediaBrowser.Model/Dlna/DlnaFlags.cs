using System;

namespace MediaBrowser.Model.Dlna
{
    [Flags]
    public enum DlnaFlags : ulong
    {
        BackgroundTransferMode = (1 << 22),
        ByteBasedSeek = (1 << 29),
        ConnectionStall = (1 << 21),
        DlnaV15 = (1 << 20),
        InteractiveTransferMode = (1 << 23),
        PlayContainer = (1 << 28),
        RtspPause = (1 << 25),
        S0Increase = (1 << 27),
        SenderPaced = (1L << 31),
        SnIncrease = (1 << 26),
        StreamingTransferMode = (1 << 24),
        TimeBasedSeek = (1 << 30)
    }
}