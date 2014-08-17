using MediaBrowser.Model.Extensions;
using System;

namespace MediaBrowser.Model.Dlna
{
    public class SearchCriteria
    {
        public SearchType SearchType { get; set; }

        public SearchCriteria(string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                throw new ArgumentNullException("search");
            }

            SearchType = SearchType.Unknown;

            String[] factors = StringHelper.RegexSplit(search, "(and|or)");
            foreach (String factor in factors)
            {
                String[] subFactors = StringHelper.RegexSplit(factor.Trim().Trim('(').Trim(')').Trim(), "\\s", 3);

                if (subFactors.Length == 3)
                {

                    if (StringHelper.EqualsIgnoreCase("upnp:class", subFactors[0]) && 
                        (StringHelper.EqualsIgnoreCase("=", subFactors[1]) || StringHelper.EqualsIgnoreCase("derivedfrom", subFactors[1])))
                    {
                        if (StringHelper.EqualsIgnoreCase("\"object.item.imageItem\"", subFactors[2]) || StringHelper.EqualsIgnoreCase("\"object.item.imageItem.photo\"", subFactors[2]))
                        {
                            SearchType = SearchType.Image;
                        }
                        else if (StringHelper.EqualsIgnoreCase("\"object.item.videoItem\"", subFactors[2]))
                        {
                            SearchType = SearchType.Video;
                        }
                        else if (StringHelper.EqualsIgnoreCase("\"object.container.playlistContainer\"", subFactors[2]))
                        {
                            SearchType = SearchType.Playlist;
                        }
                        else if (StringHelper.EqualsIgnoreCase("\"object.container.album.musicAlbum\"", subFactors[2]))
                        {
                            SearchType = SearchType.MusicAlbum;
                        }
                    }
                }
            }
        }
    }
}
