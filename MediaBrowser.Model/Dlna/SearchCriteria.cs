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

            if (StringHelper.IndexOfIgnoreCase(search, "upnp:class") != -1 &&
                StringHelper.IndexOfIgnoreCase(search, "derivedfrom") != -1)
            {
                if (StringHelper.IndexOfIgnoreCase(search, "object.item.audioItem") != -1)
                {
                    SearchType = SearchType.Audio;
                }
                else if (StringHelper.IndexOfIgnoreCase(search, "object.item.imageItem") != -1)
                {
                    SearchType = SearchType.Image;
                }
                else if (StringHelper.IndexOfIgnoreCase(search, "object.item.videoItem") != -1)
                {
                    SearchType = SearchType.Video;
                }
                else if (StringHelper.IndexOfIgnoreCase(search, "object.container.playlistContainer") != -1)
                {
                    SearchType = SearchType.Playlist;
                }
            }
        }
    }
}
