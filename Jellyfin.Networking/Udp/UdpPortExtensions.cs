using System;

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
                range.Min = UdpHelper.UdpLowerUserPort;
                range.Max = UdpHelper.UdpMaxPort;
                return false;
            }

            // Remove all white space.
            rangeStr = string.Join(string.Empty, rangeStr.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));

            var parts = rangeStr.Split('-');
            if (parts.Length == 2)
            {
                int minVal;
                int maxVal = string.IsNullOrEmpty(parts[1])
                    ? UdpHelper.UdpMaxPort
                    : Math.Clamp(
                        int.TryParse(parts[1], out int max)
                            ? max
                            : UdpHelper.UdpMaxPort,
                        UdpHelper.UdpMinPort,
                        UdpHelper.UdpMaxPort);

                if (string.IsNullOrEmpty(parts[0]))
                {
                    minVal = maxVal <= UdpHelper.UdpLowerUserPort ? UdpHelper.UdpMinPort : UdpHelper.UdpLowerUserPort;
                }
                else
                {
                    minVal = Math.Clamp(int.TryParse(parts[0], out int min) ? min : UdpHelper.UdpLowerUserPort, UdpHelper.UdpMinPort, UdpHelper.UdpMaxPort);
                }

                range.Max = maxVal;
                range.Min = minVal;
                return maxVal >= minVal;
            }

            if (int.TryParse(rangeStr, out int start))
            {
                if (start < UdpHelper.UdpMinPort || start > UdpHelper.UdpMaxPort)
                {
                    range.Min = range.Max = UdpHelper.UdpAnyPort;
                    return false;
                }

                range.Min = range.Max = start;
                return true;
            }

            // Random Port in user range.
            range.Min = UdpHelper.UdpLowerUserPort;
            range.Max = UdpHelper.UdpMaxPort;
            return false;
        }
    }
}
