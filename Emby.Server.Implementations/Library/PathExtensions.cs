#nullable enable

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
        /// <param name="attribute">The attrib.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentException"><paramref name="str" /> or <paramref name="attribute" /> is empty.</exception>
        public static string? GetAttributeValue(this string str, string attribute)
        {
            if (str.Length == 0)
            {
                throw new ArgumentException("String can't be empty.", nameof(str));
            }

            if (attribute.Length == 0)
            {
                throw new ArgumentException("String can't be empty.", nameof(attribute));
            }

            string srch = "[" + attribute + "=";
            int start = str.IndexOf(srch, StringComparison.OrdinalIgnoreCase);
            if (start != -1)
            {
                start += srch.Length;
                int end = str.IndexOf(']', start);
                return str.Substring(start, end - start);
            }

            // for imdbid we also accept pattern matching
            if (string.Equals(attribute, "imdbid", StringComparison.OrdinalIgnoreCase))
            {
                var m = Regex.Match(str, "tt([0-9]{7,8})", RegexOptions.IgnoreCase);
                return m.Success ? m.Value : null;
            }

            return null;
        }
    }
}
