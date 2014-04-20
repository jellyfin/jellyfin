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

            if (search.IndexOf("upnp:class", StringComparison.OrdinalIgnoreCase) != -1 &&
                search.IndexOf("derivedfrom", StringComparison.OrdinalIgnoreCase) != -1)
            {
                if (search.IndexOf("object.item.audioItem", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    SearchType = SearchType.Audio;
                }
                else if (search.IndexOf("object.item.imageItem", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    SearchType = SearchType.Image;
                }
                else if (search.IndexOf("object.item.videoItem", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    SearchType = SearchType.Video;
                }
                else if (search.IndexOf("object.container.playlistContainer", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    SearchType = SearchType.Playlist;
                }
            }
        }
    }

    public enum SearchType
    {
        Unknown = 0,
        Audio = 1,
        Image = 2,
        Video = 3,
        Playlist = 4
    }
}
