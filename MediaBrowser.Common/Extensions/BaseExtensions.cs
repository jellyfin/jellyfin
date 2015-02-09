using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Class BaseExtensions
    /// </summary>
    public static class BaseExtensions
    {

        /// <summary>
        /// Strips the HTML.
        /// </summary>
        /// <param name="htmlString">The HTML string.</param>
        /// <returns>System.String.</returns>
        public static string StripHtml(this string htmlString)
        {
            // http://stackoverflow.com/questions/1349023/how-can-i-strip-html-from-text-in-net
            const string pattern = @"<(.|\n)*?>";

            return Regex.Replace(htmlString, pattern, string.Empty).Trim();
        }

        public static string RemoveDiacritics(this string text)
        {
            return String.Concat(
                text.Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                              UnicodeCategory.NonSpacingMark)
              ).Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Gets the M d5.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>Guid.</returns>
        public static Guid GetMD5(this string str)
        {
            using (var provider = MD5.Create())
            {
                return new Guid(provider.ComputeHash(Encoding.Unicode.GetBytes(str)));
            }
        }

        /// <summary>
        /// Gets the MB id.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="type">The type.</param>
        /// <returns>Guid.</returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        [Obsolete("Use LibraryManager.GetNewItemId")]
        public static Guid GetMBId(this string str, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            var key = type.FullName + str.ToLower();

            return key.GetMD5();
        }
    }
}
