using MediaBrowser.Controller.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelItemInfo
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public ChannelItemType Type { get; set; }

        public string OfficialRating { get; set; }

        public string Overview { get; set; }

        public List<string> Genres { get; set; }

        public List<PersonInfo> People { get; set; }
        
        public float? CommunityRating { get; set; }

        public long? RunTimeTicks { get; set; }

        public bool IsInfinite { get; set; }
        
        public string ImageUrl { get; set; }

        public ChannelMediaType MediaType { get; set; }

        public ChannelMediaContentType ContentType { get; set; }
        
        public ChannelItemInfo()
        {
            Genres = new List<string>();
            People = new List<PersonInfo>();
        }
    }

    public enum ChannelItemType
    {
        Media = 0,

        Category = 1
    }

    public enum ChannelMediaType
    {
        Audio = 0,

        Video = 1
    }

    public enum ChannelMediaContentType
    {
        Clip = 0,

        Podcast = 1,

        Trailer = 2,

        Movie = 3,

        Episode = 4
    }
}
