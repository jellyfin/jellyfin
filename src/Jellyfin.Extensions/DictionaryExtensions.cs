using System.Collections.Generic;

namespace Jellyfin.Extensions
{
    /// <summary>
    /// Static extensions for the <see cref="IReadOnlyDictionary{TKey,TValue}"/> interface.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Gets a string from a string dictionary, checking all keys sequentially,
        /// stopping at the first key that returns a result that's neither null nor blank.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key1">The first checked key.</param>
        /// <returns>System.String.</returns>
        public static string? GetFirstNotNullNorWhiteSpaceValue(this IReadOnlyDictionary<string, string> dictionary, string key1) => GetFirstNotNullNorWhiteSpaceValue(dictionary, key1);

        /// <summary>
        /// Gets a string from a string dictionary, checking all keys sequentially,
        /// stopping at the first key that returns a result that's neither null nor blank.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key1">The first checked key.</param>
        /// <param name="key2">The second checked key.</param>
        /// <returns>System.String.</returns>
        public static string? GetFirstNotNullNorWhiteSpaceValue(this IReadOnlyDictionary<string, string> dictionary, string key1, string key2) => GetFirstNotNullNorWhiteSpaceValue(dictionary, key1, key2);

        /// <summary>
        /// Gets a string from a string dictionary, checking all keys sequentially,
        /// stopping at the first key that returns a result that's neither null nor blank.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key1">The first checked key.</param>
        /// <param name="key2">The second checked key.</param>
        /// <param name="key3">The third checked key.</param>
        /// <returns>System.String.</returns>
        public static string? GetFirstNotNullNorWhiteSpaceValue(this IReadOnlyDictionary<string, string> dictionary, string key1, string key2, string key3) => GetFirstNotNullNorWhiteSpaceValue(dictionary, key1, key2, key3);

        /// <summary>
        /// Gets a string from a string dictionary, checking all keys sequentially,
        /// stopping at the first key that returns a result that's neither null nor blank.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="keys">Keys to check, starting at first parameter.</param>
        /// <returns>System.String.</returns>
        public static string? GetFirstNotNullNorWhiteSpaceValue(this IReadOnlyDictionary<string, string> dictionary, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!string.IsNullOrEmpty(key) && dictionary.TryGetValue(key, out string? val) && !string.IsNullOrWhiteSpace(val))
                {
                    return val;
                }
            }

            return null;
        }
    }
}
