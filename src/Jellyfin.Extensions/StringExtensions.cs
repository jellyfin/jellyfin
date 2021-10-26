using System;

namespace Jellyfin.Extensions
{
    /// <summary>
    /// Provides extensions methods for <see cref="string" />.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Counts the number of occurrences of [needle] in the string.
        /// </summary>
        /// <param name="value">The haystack to search in.</param>
        /// <param name="needle">The character to search for.</param>
        /// <returns>The number of occurrences of the [needle] character.</returns>
        public static int Count(this ReadOnlySpan<char> value, char needle)
        {
            var count = 0;
            var length = value.Length;
            for (var i = 0; i < length; i++)
            {
                if (value[i] == needle)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Returns the part on the left of the <c>needle</c>.
        /// </summary>
        /// <param name="haystack">The string to seek.</param>
        /// <param name="needle">The needle to find.</param>
        /// <returns>The part left of the <paramref name="needle" />.</returns>
        public static ReadOnlySpan<char> LeftPart(this ReadOnlySpan<char> haystack, char needle)
        {
            var pos = haystack.IndexOf(needle);
            return pos == -1 ? haystack : haystack[..pos];
        }

        /// <summary>
        /// Returns the part on the right of the <c>needle</c>.
        /// </summary>
        /// <param name="haystack">The string to seek.</param>
        /// <param name="needle">The needle to find.</param>
        /// <returns>The part right of the <paramref name="needle" />.</returns>
        public static ReadOnlySpan<char> RightPart(this ReadOnlySpan<char> haystack, char needle)
        {
            var pos = haystack.LastIndexOf(needle);
            if (pos == -1)
            {
                return haystack;
            }

            if (pos == haystack.Length - 1)
            {
                return ReadOnlySpan<char>.Empty;
            }

            return haystack[(pos + 1)..];
        }
    }
}
