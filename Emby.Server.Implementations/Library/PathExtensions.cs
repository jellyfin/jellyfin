using System;
using System.Text.RegularExpressions;

namespace Emby.Server.Implementations.Library
{
    /// <summary>
    /// Class providing extension methods for working with paths.
    /// </summary>
    public static class PathExtensions
    {
        /// <summary>
        /// Gets the attribute value.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="attrib">The attrib.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">attrib</exception>
        public static string GetAttributeValue(this string str, string attrib)
        {
            if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentNullException(nameof(str));
            }

            if (string.IsNullOrEmpty(attrib))
            {
                throw new ArgumentNullException(nameof(attrib));
            }

            string srch = "[" + attrib + "=";
            int start = str.IndexOf(srch, StringComparison.OrdinalIgnoreCase);
            if (start > -1)
            {
                start += srch.Length;
                int end = str.IndexOf(']', start);
                return str.Substring(start, end - start);
            }

            // for imdbid we also accept pattern matching
            if (string.Equals(attrib, "imdbid", StringComparison.OrdinalIgnoreCase))
            {
                var m = Regex.Match(str, "tt\\d{7}", RegexOptions.IgnoreCase);
                return m.Success ? m.Value : null;
            }

            return null;
        }
    }
}
