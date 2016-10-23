using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MediaBrowser.Controller.Extensions
{
    /// <summary>
    /// Class BaseExtensions
    /// </summary>
    public static class StringExtensions
    {
        public static string RemoveDiacritics(this string text)
        {
            return String.Concat(
                text.Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                              UnicodeCategory.NonSpacingMark)
              ).Normalize(NormalizationForm.FormC);
        }
    }
}
