using System;
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
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var chars = Normalize(text, NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark);

            return Normalize(string.Concat(chars), NormalizationForm.FormC);
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
                    text = Regex.Replace(text, "([\ud800-\udbff](?![\udc00-\udfff]))|((?<![\ud800-\udbff])[\udc00-\udfff])", "");
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
