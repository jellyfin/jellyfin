using System;
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

        /// <summary>
        /// Replaces the specified STR.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        /// <param name="comparison">The comparison.</param>
        /// <returns>System.String.</returns>
        public static string Replace(this string str, string oldValue, string newValue, StringComparison comparison)
        {
            var sb = new StringBuilder();

            var previousIndex = 0;
            var index = str.IndexOf(oldValue, comparison);

            while (index != -1)
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;

                previousIndex = index;
                index = str.IndexOf(oldValue, index, comparison);
            }

            sb.Append(str.Substring(previousIndex));

            return sb.ToString();
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
        public static Guid GetMBId(this string str, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            var key = type.FullName + str.ToLower();

            return key.GetMD5();
        }

        /// <summary>
        /// Gets the attribute value.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="attrib">The attrib.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">attrib</exception>
        public static string GetAttributeValue(this string str, string attrib)
        {
            if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentNullException("str");
            }

            if (string.IsNullOrEmpty(attrib))
            {
                throw new ArgumentNullException("attrib");
            }
            
            string srch = "[" + attrib + "=";
            int start = str.IndexOf(srch, StringComparison.OrdinalIgnoreCase);
            if (start > -1)
            {
                start += srch.Length;
                int end = str.IndexOf(']', start);
                return str.Substring(start, end - start);
            }
            return null;
        }
    }
}
