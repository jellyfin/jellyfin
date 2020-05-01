#pragma warning disable CS1591

namespace MediaBrowser.Model.Dlna
{
    public static class DlnaMaps
    {
        private static readonly string DefaultStreaming =
             FlagsToString(DlnaFlags.StreamingTransferMode |
                           DlnaFlags.BackgroundTransferMode |
                           DlnaFlags.ConnectionStall |
                           DlnaFlags.ByteBasedSeek |
                           DlnaFlags.DlnaV15);

        private static readonly string DefaultInteractive =
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
                string orgOp = string.Empty;

                // Time-based seeking currently only possible when transcoding
                orgOp += isDirectStream ? "0" : "1";

                // Byte-based seeking only possible when not transcoding
                orgOp += isDirectStream || profileTranscodeSeekInfo == TranscodeSeekInfo.Bytes ? "1" : "0";

                return orgOp;
            }

            // No seeking is available if we don't know the content runtime
            return "00";
        }

        public static string GetImageOrgOpValue()
        {
            string orgOp = string.Empty;

            // Time-based seeking currently only possible when transcoding
            orgOp += "0";

            // Byte-based seeking only possible when not transcoding
            orgOp += "0";

            return orgOp;
        }
    }
}
