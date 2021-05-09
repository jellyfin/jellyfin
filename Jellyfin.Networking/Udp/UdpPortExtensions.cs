using System;
using System.Text.RegularExpressions;

namespace Jellyfin.Networking.Udp
{
    /// <summary>
    /// Defines the <see cref="UdpPortExtensions" />.
    /// </summary>
    public static class UdpPortExtensions
    {
        /// <summary>
        /// Parses a string and returns a range value if possible. If an invalid range is provided, the user range is provided.
        /// </summary>
        /// <param name="rangeStr">String to parse.</param>
        /// <param name="range">Range value contained in rangeStr.</param>
        /// <returns>Result of the operation.</returns>
        public static bool TryParseRange(this string rangeStr, out (int Min, int Max) range)
        {
            if (string.IsNullOrEmpty(rangeStr))
            {
                // Random Port.
                range.Min = UdpHelper.UDPLowerUserPort;
                range.Max = UdpHelper.UDPMaxPort;
                return false;
            }

            // Remove all white space.
            rangeStr = Regex.Replace(rangeStr, @"\s+", string.Empty);

            var parts = rangeStr.Split('-');
            if (parts.Length == 2)
            {
                int minVal;
                int maxVal;

                if (string.IsNullOrEmpty(parts[1]))
                {
                    maxVal = UdpHelper.UDPMaxPort;
                }
                else
                {
                    maxVal = Math.Clamp(int.TryParse(parts[1], out int max) ? max : UdpHelper.UDPMaxPort, UdpHelper.UDPMinPort, UdpHelper.UDPMaxPort);
                }

                if (string.IsNullOrEmpty(parts[0]))
                {
                    minVal = maxVal <= UdpHelper.UDPLowerUserPort ? UdpHelper.UDPMinPort : UdpHelper.UDPLowerUserPort;
                }
                else
                {
                    minVal = Math.Clamp(int.TryParse(parts[0], out int min) ? min : UdpHelper.UDPLowerUserPort, UdpHelper.UDPMinPort, UdpHelper.UDPMaxPort);
                }

                range.Max = maxVal;
                range.Min = minVal;
                return maxVal >= minVal;
            }

            if (int.TryParse(rangeStr, out int start))
            {
                if (start < UdpHelper.UDPMinPort || start > UdpHelper.UDPMaxPort)
                {
                    range.Min = range.Max = UdpHelper.UDPAnyPort;
                    return false;
                }

                range.Min = range.Max = start;
                return true;
            }

            // Random Port in user range.
            range.Min = UdpHelper.UDPLowerUserPort;
            range.Max = UdpHelper.UDPMaxPort;
            return false;
        }
    }
}
