using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasTags
    /// </summary>
    public interface IHasTags
    {
        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        List<string> Tags { get; set; }
    }

    public static class TagExtensions
    {
        public static void AddTag(this IHasTags item, string name)
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
