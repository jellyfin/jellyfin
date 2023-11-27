using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Class BaseExtensions.
    /// </summary>
    public static partial class BaseExtensions
    {
        // http://stackoverflow.com/questions/1349023/how-can-i-strip-html-from-text-in-net
        [GeneratedRegex(@"<(.|\n)*?>")]
        private static partial Regex StripHtmlRegex();

        /// <summary>
        /// Strips the HTML.
        /// </summary>
        /// <param name="htmlString">The HTML string.</param>
        /// <returns><see cref="string" />.</returns>
        public static string StripHtml(this string htmlString)
            => StripHtmlRegex().Replace(htmlString, string.Empty).Trim();

        /// <summary>
        /// Gets the Md5.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns><see cref="Guid" />.</returns>
        public static Guid GetMD5(this string str)
        {
#pragma warning disable CA5351
            return new Guid(MD5.HashData(Encoding.Unicode.GetBytes(str)));
#pragma warning restore CA5351
        }
    }
}
