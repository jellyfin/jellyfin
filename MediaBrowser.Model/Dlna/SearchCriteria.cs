#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Text.RegularExpressions;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Model.Dlna
{
    public class SearchCriteria
    {
        public SearchType SearchType { get; set; }

        /// <summary>
        /// Splits the specified string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="term">The term.</param>
        /// <param name="limit">The limit.</param>
        /// <returns>System.String[].</returns>
        private static string[] RegexSplit(string str, string term, int limit)
        {
            return new Regex(term).Split(str, limit);
        }

        /// <summary>
        /// Splits the specified string.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="term">The term.</param>
        /// <returns>System.String[].</returns>
        private static string[] RegexSplit(string str, string term)
        {
            return Regex.Split(str, term, RegexOptions.IgnoreCase);
        }

        public SearchCriteria(string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                throw new ArgumentNullException(nameof(search));
            }

            SearchType = SearchType.Unknown;

            string[] factors = RegexSplit(search, "(and|or)");
            foreach (string factor in factors)
            {
                string[] subFactors = RegexSplit(factor.Trim().Trim('(').Trim(')').Trim(), "\\s", 3);

                if (subFactors.Length == 3)
                {

                    if (string.Equals("upnp:class", subFactors[0], StringComparison.OrdinalIgnoreCase) &&
                        (string.Equals("=", subFactors[1]) || string.Equals("derivedfrom", subFactors[1], StringComparison.OrdinalIgnoreCase)))
                    {
                        if (string.Equals("\"object.item.imageItem\"", subFactors[2]) || string.Equals("\"object.item.imageItem.photo\"", subFactors[2], StringComparison.OrdinalIgnoreCase))
                        {
                            SearchType = SearchType.Image;
                        }
                        else if (string.Equals("\"object.item.videoItem\"", subFactors[2], StringComparison.OrdinalIgnoreCase))
                        {
                            SearchType = SearchType.Video;
                        }
                        else if (string.Equals("\"object.container.playlistContainer\"", subFactors[2], StringComparison.OrdinalIgnoreCase))
                        {
                            SearchType = SearchType.Playlist;
                        }
                        else if (string.Equals("\"object.container.album.musicAlbum\"", subFactors[2], StringComparison.OrdinalIgnoreCase))
                        {
                            SearchType = SearchType.MusicAlbum;
                        }
                    }
                }
            }
        }
    }
}
