using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Jellyfin.Extensions
{
    /// <summary>
    /// Provides extensions methods for <see cref="string" />.
    /// </summary>
    public static class StringExtensions
    {
        // Matches non-conforming unicode chars
        // https://mnaoumov.wordpress.com/2014/06/14/stripping-invalid-characters-from-utf-16-strings/
        private static readonly Regex _nonConformingUnicode = new Regex("([\ud800-\udbff](?![\udc00-\udfff]))|((?<![\ud800-\udbff])[\udc00-\udfff])|(\ufffd)");

        /// <summary>
        /// Removes the diacritics character from the strings.
        /// </summary>
        /// <param name="text">The string to act on.</param>
        /// <returns>The string without diacritics character.</returns>
        public static string RemoveDiacritics(this string text)
        {
            string withDiactritics = _nonConformingUnicode
                .Replace(text, string.Empty)
                .Normalize(NormalizationForm.FormD);

            var withoutDiactritics = new StringBuilder();
            foreach (char c in withDiactritics)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    withoutDiactritics.Append(c);
                }
            }

            return withoutDiactritics.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Checks whether or not the specified string has diacritics in it.
        /// </summary>
        /// <param name="text">The string to check.</param>
        /// <returns>True if the string has diacritics, false otherwise.</returns>
        public static bool HasDiacritics(this string text)
        {
            return !string.Equals(text, text.RemoveDiacritics(), StringComparison.Ordinal);
        }

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
