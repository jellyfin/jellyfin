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
        public static string? GetFirstNotNullNorWhiteSpaceValue(this IReadOnlyDictionary<string, string> dictionary, string key1)
        {
            return dictionary.GetFirstNotNullNorWhiteSpaceValue(key1, string.Empty, string.Empty);
        }

        /// <summary>
        /// Gets a string from a string dictionary, checking all keys sequentially,
        /// stopping at the first key that returns a result that's neither null nor blank.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key1">The first checked key.</param>
        /// <param name="key2">The second checked key.</param>
        /// <returns>System.String.</returns>
        public static string? GetFirstNotNullNorWhiteSpaceValue(this IReadOnlyDictionary<string, string> dictionary, string key1, string key2)
        {
            return dictionary.GetFirstNotNullNorWhiteSpaceValue(key1, key2, string.Empty);
        }

        /// <summary>
        /// Gets a string from a string dictionary, checking all keys sequentially,
        /// stopping at the first key that returns a result that's neither null nor blank.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key1">The first checked key.</param>
        /// <param name="key2">The second checked key.</param>
        /// <param name="key3">The third checked key.</param>
        /// <returns>System.String.</returns>
        public static string? GetFirstNotNullNorWhiteSpaceValue(this IReadOnlyDictionary<string, string> dictionary, string key1, string key2, string key3)
        {
            if (dictionary.TryGetValue(key1, out var val) && !string.IsNullOrWhiteSpace(val))
            {
                return val;
            }

            if (!string.IsNullOrEmpty(key2) && dictionary.TryGetValue(key2, out val) && !string.IsNullOrWhiteSpace(val))
            {
                return val;
            }

            if (!string.IsNullOrEmpty(key3) && dictionary.TryGetValue(key3, out val) && !string.IsNullOrWhiteSpace(val))
            {
                return val;
            }

            return null;
        }
    }
}
