using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MediaBrowser.Net
{
    public static class CollectionExtensions
    {
        public static IDictionary<string, IEnumerable<string>> ToDictionary(this NameValueCollection source)
        {
            return source.AllKeys.ToDictionary<string, string, IEnumerable<string>>(key => key, source.GetValues);
        }
    }
}