#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace Emby.Dlna
{
    /// <summary>
    /// Provides SSDP message parsing and building functionality.
    /// </summary>
    public static class SsdpMessageHelper
    {
        /// <summary>
        /// Builds an SSDP message.
        /// </summary>
        /// <param name="header">SSDP Header string.</param>
        /// <param name="values">SSDP paramaters.</param>
        /// <returns>Formatted string.</returns>
        public static string BuildMessage(string header, Dictionary<string, string> values)
        {
            const string SsdpOpt = "\"http://schemas.upnp.org/upnp/1/0/\"; ns={";

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            values[DlnaEntryPoint.Instance.NetworkChangeCount + "-NLS"] = DlnaEntryPoint.NetworkLocationSignature;
            values["OPT"] = SsdpOpt + DlnaEntryPoint.Instance.NetworkChangeCount;
            values["SERVER"] = DlnaEntryPoint.Name;

            var builder = new StringBuilder();

            builder.AppendFormat(CultureInfo.InvariantCulture, "{0}\r\n", header);

            foreach (var pair in values)
            {
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}\r\n", pair.Key, pair.Value);
            }

            builder.Append("\r\n");

            return builder.ToString();
        }

        /// <summary>
        /// Returns a Header from the collection.
        /// </summary>
        /// <param name="headerName">Name to look for.</param>
        /// <param name="headers">Collection to search.</param>
        /// <returns>Value of the property.</returns>
        public static Uri? GetFirstHeaderUriValue(string headerName, HttpHeaders headers)
        {
            if (headers == null)
            {
                return null;
            }

            string value = string.Empty;
            if (headers.TryGetValues(headerName, out IEnumerable<string> values) && values != null)
            {
                value = values.FirstOrDefault();
            }

            if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri retVal))
            {
                return retVal;
            }

            return null;
        }

        /// <summary>
        /// Returns a Header from the collection.
        /// </summary>
        /// <param name="headerName">Name to look for.</param>
        /// <param name="headers">Collection to search.</param>
        /// <returns>Value of the property.</returns>
        public static string GetFirstHeaderValue(string headerName, HttpHeaders headers)
        {
            if (headers == null)
            {
                return string.Empty;
            }

            string retVal = string.Empty;
            if (headers.TryGetValues(headerName, out IEnumerable<string> values) && values != null)
            {
                retVal = values.FirstOrDefault();
            }

            return retVal;
        }

        /// <summary>
        /// Extracts the cache age from the headers.
        /// </summary>
        /// <param name="headerValue">CacheControlHeaderValue instance to parse.</param>
        /// <returns>The age of the header.</returns>
        public static TimeSpan CacheAgeFromHeader(CacheControlHeaderValue headerValue)
        {
            if (headerValue == null)
            {
                return TimeSpan.Zero;
            }

            return headerValue.MaxAge ?? headerValue.SharedMaxAge ?? TimeSpan.Zero;
        }
    }
}
