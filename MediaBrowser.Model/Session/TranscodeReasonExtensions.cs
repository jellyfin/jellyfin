using System;
using System.Linq;

namespace MediaBrowser.Model.Session
{
    /// <summary>
    /// Extension methods for serializing TranscodeReason.
    /// </summary>
    public static class TranscodeReasonExtensions
    {
        private static readonly TranscodeReason[] _values = Enum.GetValues<TranscodeReason>();

        /// <summary>
        /// Serializes a TranscodeReason into a delimiter-separated string.
        /// </summary>
        /// <param name="reasons">The <see cref="TranscodeReason"/> enumeration.</param>
        /// <param name="sep">The string separator to use. defualt <c>,</c>.</param>
        /// <returns>string of transcode reasons delimited.</returns>
        public static string Serialize(this TranscodeReason reasons, string sep = ",")
        {
            return string.Join(sep, reasons.ToArray());
        }

        /// <summary>
        /// Serializes a TranscodeReason into an array of individual TranscodeReason bits.
        /// </summary>
        /// <param name="reasons">The <see cref="TranscodeReason"/> enumeration.</param>
        /// <returns>Array of <c>TranscodeReason</c>.</returns>
        public static TranscodeReason[] ToArray(this TranscodeReason reasons)
        {
            return _values.Where(r => r != 0 && reasons.HasFlag(r)).ToArray();
        }
    }
}
