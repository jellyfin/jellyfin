using System;
using System.Linq;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Request Extensions.
    /// </summary>
    public static class RequestHelpers
    {
        /// <summary>
        /// Splits a string at a separating character into an array of substrings.
        /// </summary>
        /// <param name="value">The string to split.</param>
        /// <param name="separator">The char that separates the substrings.</param>
        /// <param name="removeEmpty">Option to remove empty substrings from the array.</param>
        /// <returns>An array of the substrings.</returns>
        internal static string[] Split(string value, char separator, bool removeEmpty)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return removeEmpty
                ? value.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                : value.Split(separator);
        }

        /// <summary>
        /// Splits a comma delimited string and parses Guids.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <returns>Parsed Guids.</returns>
        public static Guid[] GetGuids(string value)
        {
            if (value == null)
            {
                return Array.Empty<Guid>();
            }

            return value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => new Guid(i))
                .ToArray();
        }
    }
}
