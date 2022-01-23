#pragma warning disable CS1591

using System;
using System.Linq;

namespace MediaBrowser.Model.Session
{
    public static class TranscodeReasonExtensions
    {
        private static TranscodeReason[] values = Enum.GetValues<TranscodeReason>();

        public static string Serialize(this MediaBrowser.Model.Session.TranscodeReason reasons, string sep = ",")
        {
            return string.Join(sep, reasons.ToArray());
        }

        public static TranscodeReason[] ToArray(this MediaBrowser.Model.Session.TranscodeReason reasons)
        {
            return values.Where(r => r != 0 && reasons.HasFlag(r)).ToArray();
        }
    }
}
