using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasKeywords
    {
        /// <summary>
        /// Gets or sets the keywords.
        /// </summary>
        /// <value>The keywords.</value>
        List<string> Keywords { get; set; }
    }

    public static class KeywordExtensions
    {
        public static void AddKeyword(this IHasKeywords item, string name)
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
