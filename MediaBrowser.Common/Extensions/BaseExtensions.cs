#nullable enable

using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Class BaseExtensions.
    /// </summary>
    public static class BaseExtensions
    {
        /// <summary>
        /// Strips the HTML.
        /// </summary>
        /// <param name="htmlString">The HTML string.</param>
        /// <returns><see cref="string" />.</returns>
        public static string StripHtml(this string htmlString)
        {
            // http://stackoverflow.com/questions/1349023/how-can-i-strip-html-from-text-in-net
            const string Pattern = @"<(.|\n)*?>";

            return Regex.Replace(htmlString, Pattern, string.Empty).Trim();
        }

        /// <summary>
        /// Gets the Md5.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns><see cref="Guid" />.</returns>
        public static Guid GetMD5(this string str)
        {
#pragma warning disable CA5351
            using (var provider = MD5.Create())
            {
                return new Guid(provider.ComputeHash(Encoding.Unicode.GetBytes(str)));
            }

#pragma warning restore CA5351
        }
    }
}
