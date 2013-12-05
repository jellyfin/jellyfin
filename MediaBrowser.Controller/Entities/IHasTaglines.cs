using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasTaglines
    /// </summary>
    public interface IHasTaglines
    {
        /// <summary>
        /// Gets or sets the taglines.
        /// </summary>
        /// <value>The taglines.</value>
        List<string> Taglines { get; set; }
    }

    public static class TaglineExtensions
    {
        /// <summary>
        /// Adds the tagline.
        /// </summary>
        /// <param name="tagline">The tagline.</param>
        /// <exception cref="System.ArgumentNullException">tagline</exception>
        public static void AddTagline(this IHasTaglines item, string tagline)
        {
            if (string.IsNullOrWhiteSpace(tagline))
            {
                throw new ArgumentNullException("tagline");
            }

            if (!item.Taglines.Contains(tagline, StringComparer.OrdinalIgnoreCase))
            {
                item.Taglines.Add(tagline);
            }
        }
    }
}
