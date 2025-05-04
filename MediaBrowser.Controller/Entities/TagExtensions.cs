#pragma warning disable CS1591

using System;
using System.Linq;
using Jellyfin.Extensions;

namespace MediaBrowser.Controller.Entities
{
    public static class TagExtensions
    {
        public static void AddTag(this BaseItem item, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var current = item.Tags;

            if (!current.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                if (current.Length == 0)
                {
                    item.Tags = [name];
                }
                else
                {
                    item.Tags = [..current, name];
                }
            }
        }
    }
}
