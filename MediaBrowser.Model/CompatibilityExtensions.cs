using System;

namespace MediaBrowser.Model.Extensions
{
    /// <summary>
    /// Extensions for compatibility between different framework versions.
    /// </summary>
    public static class CompatibilityExtensions
    {
        /// <summary>
        /// Wrapper for string.Replace, using conditional compilation to remove compiler warnings.
        /// </summary>
        /// <param name="s">String to use to replace.</param>
        /// <param name="searchString">String to search for.</param>
        /// <param name="replaceString">String to replace.</param>
        /// <returns>New string, with the search string replaced.</returns>
        public static string CReplace(this string s, string searchString, string replaceString)
        {
#if NETSTANDARD2_0
            return s.Replace(searchString, replaceString);
#else
            return s.Replace(searchString, replaceString, StringComparison.Ordinal);
#endif
        }

        /// <summary>
        /// Wrapper for GetHashCode(string), using conditional compilation to remove compiler
        /// warnings.
        /// </summary>
        /// <param name="s">String to get hash code for.</param>
        /// <returns>Hashcode of string.</returns>
        public static int CGetHashCode(this string s)
        {
#if NETSTANDARD2_0
            return s?.GetHashCode() ?? 0;
#else
            return s?.GetHashCode(StringComparison.Ordinal) ?? 0;
#endif
        }
    }
}
