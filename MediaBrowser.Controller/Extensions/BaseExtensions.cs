using System;
using System.Globalization;
using System.Linq;
using System.Text;
using MediaBrowser.Model.Cryptography;

namespace MediaBrowser.Controller.Extensions
{
    /// <summary>
    /// Class BaseExtensions
    /// </summary>
    public static class BaseExtensions
    {
        public static ICryptographyProvider CryptographyProvider { get; set; }

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
