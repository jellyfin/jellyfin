using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Extensions;

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

            var current = item.Tags;

            if (!current.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                if (current.Length == 0)
                {
                    item.Tags = new[] { name };
                }
                else
                {
                    var list = current.ToArray(current.Length + 1);
                    list[list.Length - 1] = name;

                    item.Tags = list;
                }
            }
        }
    }
}
