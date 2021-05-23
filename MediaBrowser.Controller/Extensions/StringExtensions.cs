#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaBrowser.Controller.Extensions
{
    /// <summary>
    /// Class BaseExtensions.
    /// </summary>
    public static class StringExtensions
    {
        public static string RemoveDiacritics(this string text)
        {
            var chars = Normalize(text, NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark);

            return Normalize(string.Concat(chars), NormalizationForm.FormC);
        }

        /// <summary>
        /// Counts the number of occurrences of [needle] in the string.
        /// </summary>
        /// <param name="value">The haystack to search in.</param>
        /// <param name="needle">The character to search for.</param>
        /// <returns>The number of occurrences of the [needle] character.</returns>
        public static int CountOccurrences(this ReadOnlySpan<char> value, char needle)
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

        private static string Normalize(string text, NormalizationForm form, bool stripStringOnFailure = true)
        {
            if (stripStringOnFailure)
            {
                try
                {
                    return text.Normalize(form);
                }
                catch (ArgumentException)
                {
                    // will throw if input contains invalid unicode chars
                    // https://mnaoumov.wordpress.com/2014/06/14/stripping-invalid-characters-from-utf-16-strings/
                    text = Regex.Replace(text, "([\ud800-\udbff](?![\udc00-\udfff]))|((?<![\ud800-\udbff])[\udc00-\udfff])", string.Empty);
                    return Normalize(text, form, false);
                }
            }

            try
            {
                return text.Normalize(form);
            }
            catch (ArgumentException)
            {
                // if it still fails, return the original text
                return text;
            }
        }
    }
}
