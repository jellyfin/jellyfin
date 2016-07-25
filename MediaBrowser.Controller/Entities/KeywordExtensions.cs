using System;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    public static class KeywordExtensions
    {
        public static void AddKeyword(this BaseItem item, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            if (!item.Keywords.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                item.Keywords.Add(name);
            }
        }
    }
}
