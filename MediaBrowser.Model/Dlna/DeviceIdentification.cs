using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="DeviceIdentification" />.
    /// </summary>
    [XmlRoot("Profile")]
    public class DeviceIdentification : DeviceDetails
    {
        // Matching weighting.
        private const int NoMatch = 0;
        private const int IpMatch = 1000;
        private const int ExactMatch = 100;
        private const int SubStringMatch = 10;
        private const int RegExMatch = 1;

        /// <summary>
        /// Gets or sets the headers.
        /// </summary>
        /// <value>The headers.</value>
        public HttpHeaderInfo[] Headers { get; set; } = Array.Empty<HttpHeaderInfo>();

        /// <summary>
        /// Compares this instance against <paramref name="headers"/>.
        /// </summary>
        /// <param name="headers">The <see cref="IHeaderDictionary"/> instance to match against.</param>
        /// <param name="addrString">The ip address of the device.</param>
        /// <returns>A weighted number representing the match, or zero if none.</returns>
        public int Matches(IHeaderDictionary headers, string addrString)
        {
            if (string.Equals(Address, addrString, StringComparison.Ordinal))
            {
                return IpMatch; // IP Match cannot be beaten by header matches.
            }

            int sum = 0;
            for (int i = 0; i < Headers.Length; i++)
            {
                var res = IsMatch(headers, Headers[i]);
                if (res == NoMatch)
                {
                    return NoMatch;
                }

                sum += res;
            }

            return sum;
        }

        /// <summary>
        /// Compares the information in <paramref name="headers"/> and <paramref name="header"/> to see if there is a match.
        /// </summary>
        /// <param name="headers">A <see cref="IHeaderDictionary"/> instance.</param>
        /// <param name="header">A <see cref="HttpHeaderInfo"/> instance.</param>
        /// <returns><c>True</c> if they match.</returns>
        private static int IsMatch(IHeaderDictionary headers, HttpHeaderInfo header)
        {
            // Handle invalid user setup
            if (string.IsNullOrEmpty(header.Name))
            {
                return NoMatch;
            }

            if (!headers.TryGetValue(header.Name, out StringValues value))
            {
                return NoMatch;
            }

            switch (header.Match)
            {
                case HeaderMatchType.Equals:
                    return string.Equals(value, header.Value, StringComparison.OrdinalIgnoreCase) ? ExactMatch : NoMatch;

                case HeaderMatchType.Substring:
                    var isMatch = value.ToString().IndexOf(header.Value, StringComparison.OrdinalIgnoreCase) != -1;
                    // _logger.LogDebug("IsMatch-Substring value: {0} testValue: {1} isMatch: {2}", value, header.Value, isMatch);
                    return isMatch ? SubStringMatch : NoMatch;

                case HeaderMatchType.Regex:
                    return Regex.IsMatch(value, header.Value, RegexOptions.IgnoreCase) ? RegExMatch : NoMatch;

                default:
                    throw new ArgumentException("Unrecognized HeaderMatchType");
            }
        }
    }
}
