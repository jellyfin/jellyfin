using System;

namespace MediaBrowser.Model.Dlna
{
    public class DlnaMaps
    {
        public static readonly string DefaultStreaming =
             FlagsToString(DlnaFlags.StreamingTransferMode |
                           DlnaFlags.BackgroundTransferMode |
                           DlnaFlags.ConnectionStall |
                           DlnaFlags.ByteBasedSeek |
                           DlnaFlags.DlnaV15);

        public static readonly string DefaultInteractive =
          FlagsToString(DlnaFlags.InteractiveTransferMode |
                        DlnaFlags.BackgroundTransferMode |
                        DlnaFlags.ConnectionStall |
                        DlnaFlags.ByteBasedSeek |
                        DlnaFlags.DlnaV15);

        public static string FlagsToString(DlnaFlags flags)
        {
            return string.Format("{0:X8}{1:D24}", (ulong)flags, 0);
        }

        public static string GetOrgOpValue(bool hasKnownRuntime, bool isDirectStream, TranscodeSeekInfo profileTranscodeSeekInfo)
        {
            if (hasKnownRuntime)
            {
                var orgOp = string.Empty;

                // Time-based seeking currently only possible when transcoding
                orgOp += isDirectStream ? "0" : "1";

                // Byte-based seeking only possible when not transcoding
                orgOp += isDirectStream || profileTranscodeSeekInfo == TranscodeSeekInfo.Bytes ? "1" : "0";

                return orgOp;
            }

            // No seeking is available if we don't know the content runtime
            return "00";
        }
    }

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
