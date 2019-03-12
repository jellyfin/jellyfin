using System.Collections.Generic;

namespace MediaBrowser.Common.Extensions
{
    // The MS CollectionExtensions are only available in netcoreapp
    public static class CollectionExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue> (this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
        {
            dictionary.TryGetValue(key, out var ret);
            return ret;
        }
    }
}
