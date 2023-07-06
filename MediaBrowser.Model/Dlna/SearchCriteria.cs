#pragma warning disable CS1591

using System;
using System.Text.RegularExpressions;

namespace MediaBrowser.Model.Dlna
{
    public partial class SearchCriteria
    {
        public SearchCriteria(string search)
        {
            ArgumentException.ThrowIfNullOrEmpty(search);

            SearchType = SearchType.Unknown;

            string[] factors = AndOrRegex().Split(search);
            foreach (string factor in factors)
            {
                string[] subFactors = WhiteSpaceRegex().Split(factor.Trim().Trim('(').Trim(')').Trim(), 3);

                if (subFactors.Length == 3)
                {
                    if (string.Equals("upnp:class", subFactors[0], StringComparison.OrdinalIgnoreCase)
                        && (string.Equals("=", subFactors[1], StringComparison.Ordinal) || string.Equals("derivedfrom", subFactors[1], StringComparison.OrdinalIgnoreCase)))
                    {
                        if (string.Equals("\"object.item.imageItem\"", subFactors[2], StringComparison.Ordinal) || string.Equals("\"object.item.imageItem.photo\"", subFactors[2], StringComparison.OrdinalIgnoreCase))
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

        public SearchType SearchType { get; set; }

        [GeneratedRegex("\\s")]
        private static partial Regex WhiteSpaceRegex();

        [GeneratedRegex("(and|or)", RegexOptions.IgnoreCase)]
        private static partial Regex AndOrRegex();
    }
}
