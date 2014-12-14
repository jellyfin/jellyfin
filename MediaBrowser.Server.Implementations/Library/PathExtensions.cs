using System;

namespace MediaBrowser.Server.Implementations.Library
{
    public static class PathExtensions
    {
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
