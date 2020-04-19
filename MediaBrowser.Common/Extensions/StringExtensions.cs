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
        /// Returns the part left of the <c>needle</c>.
        /// </summary>
        /// <param name="str">The string to seek.</param>
        /// <param name="needle">The needle to find.</param>
        /// <returns>The part left of the <c>needle</c>.</returns>
        public static ReadOnlySpan<char> LeftPart(this ReadOnlySpan<char> str, char needle)
        {
            var pos = str.IndexOf(needle);
            return pos == -1 ? str : str[..pos];
        }

        /// <summary>
        /// Returns the part left of the <c>needle</c>.
        /// </summary>
        /// <param name="str">The string to seek.</param>
        /// <param name="needle">The needle to find.</param>
        /// <param name="stringComparison">One of the enumeration values that specifies the rules for the search.</param>
        /// <returns>The part left of the <c>needle</c>.</returns>
        public static ReadOnlySpan<char> LeftPart(this ReadOnlySpan<char> str, ReadOnlySpan<char> needle, StringComparison stringComparison = default)
        {
            var pos = str.IndexOf(needle, stringComparison);
            return pos == -1 ? str : str[..pos];
        }
    }
}
