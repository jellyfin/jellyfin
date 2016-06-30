using System;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    public static class TagExtensions
    {
        public static void AddTag(this BaseItem item, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            if (!item.Tags.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                item.Tags.Add(name);
            }
        }
    }
}
