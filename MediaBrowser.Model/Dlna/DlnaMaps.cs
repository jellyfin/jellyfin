using System.Globalization;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="DlnaMaps" />.
    /// </summary>
    public static class DlnaMaps
    {
        /// <summary>
        /// Converts Dlna flags to a string.
        /// </summary>
        /// <param name="flags">The <see cref="DlnaFlags"/>.</param>
        /// <returns>The string equivalent.</returns>
        public static string FlagsToString(DlnaFlags flags)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:X8}{1:D24}", (ulong)flags, 0);
        }

        /// <summary>
        /// Gets the OrgOp value.
        /// </summary>
        /// <param name="hasKnownRuntime">True if it has a known runtime.</param>
        /// <param name="isDirectStream">True is it is direct stream.</param>
        /// <param name="profileTranscodeSeekInfo">The <see cref="TranscodeSeekInfo"/>.</param>
        /// <returns>The OrgOp..</returns>
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

        /// <summary>
        /// Gets the image OrgOp value.
        /// </summary>
        /// <returns>The OrgOp value>.</returns>
        public static string GetImageOrgOpValue()
        {
            return "00";
            // string orgOp = string.Empty;

            // Time-based seeking currently only possible when transcoding
            // orgOp += "0";

            // Byte-based seeking only possible when not transcoding
            // orgOp += "0";

            // return orgOp;
        }
    }
}
