using System;
using System.Text.RegularExpressions;

namespace Jellyfin.Networking.Udp
{
    /// <summary>
    /// Defines the <see cref="StringExtensions" />.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Parses a string and returns a range value if possible.
        /// </summary>
        /// <param name="rangeStr">String to parse.</param>
        /// <param name="range">Range value contained in rangeStr.</param>
        /// <returns>Result of the operation.</returns>
        public static bool TryParseRange(this string rangeStr, out (int Min, int Max) range)
        {
            if (string.IsNullOrEmpty(rangeStr))
            {
                // Random Port.
                range.Min = 1;
                range.Max = 65535;
                return false;
            }

            // Remove all white space.
            rangeStr = Regex.Replace(rangeStr, @"\s+", string.Empty);

            var parts = rangeStr.Split('-');
            if (parts.Length == 2)
            {
                int minVal = int.TryParse(parts[0], out int min) ? min : 1;
                int maxVal = int.TryParse(parts[1], out int max) ? max : 65535;
                if (minVal < 1)
                {
                    minVal = 1;
                }

                if (maxVal > 65535)
                {
                    maxVal = 65535;
                }

                range.Max = Math.Max(minVal, maxVal);
                range.Min = Math.Min(minVal, maxVal);
                return true;
            }

            if (int.TryParse(rangeStr, out int start))
            {
                if (start < 1 || start > 65535)
                {
                    start = 0; // Random Port.
                }

                range.Min = range.Max = start;
                return true;
            }

            // Random Port.
            range.Min = 1;
            range.Max = 65535;
            return false;
        }
    }
}
