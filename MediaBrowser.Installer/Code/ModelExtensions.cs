
using System.Collections.Generic;

namespace MediaBrowser.Installer.Code
{
    /// <summary>
    /// Class ModelExtensions
    /// </summary>
    static class ModelExtensions
    {
        /// <summary>
        /// Values the or default.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="def">The def.</param>
        /// <returns>System.String.</returns>
        public static string ValueOrDefault(this string str, string def = "")
        {
            return string.IsNullOrEmpty(str) ? def : str;
        }

        /// <summary>
        /// Helper method for Dictionaries since they throw on not-found keys
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>``1.</returns>
        public static U GetValueOrDefault<T, U>(this Dictionary<T, U> dictionary, T key, U defaultValue)
        {
            U val;
            if (!dictionary.TryGetValue(key, out val))
            {
                val = defaultValue;
            }
            return val;

        }

    }
}
