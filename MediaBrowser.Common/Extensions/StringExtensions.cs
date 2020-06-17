#nullable enable

using System;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Extensions methods to simplify string operations.
    /// </summary>
    public static class StringExtensions
    {
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
        /// Returns the part on the left of the <c>needle</c>.
        /// </summary>
        /// <param name="haystack">The string to seek.</param>
        /// <param name="needle">The needle to find.</param>
        /// <param name="stringComparison">One of the enumeration values that specifies the rules for the search.</param>
        /// <returns>The part left of the <c>needle</c>.</returns>
        public static ReadOnlySpan<char> LeftPart(this ReadOnlySpan<char> haystack, ReadOnlySpan<char> needle, StringComparison stringComparison = default)
        {
            var pos = haystack.IndexOf(needle, stringComparison);
            return pos == -1 ? haystack : haystack[..pos];
        }
    }
}
