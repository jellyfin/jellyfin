using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelItemInfo : IHasProviderIds
    {
        public string Name { get; set; }

        public string Id { get; set; }

        public ChannelItemType Type { get; set; }

        public string OfficialRating { get; set; }

        public string Overview { get; set; }

        public List<string> Genres { get; set; }
        public List<string> Studios { get; set; }

        public List<PersonInfo> People { get; set; }
        
        public float? CommunityRating { get; set; }

        public long? RunTimeTicks { get; set; }

        public bool IsInfiniteStream { get; set; }
        
        public string ImageUrl { get; set; }

        public ChannelMediaType MediaType { get; set; }

        public ChannelMediaContentType ContentType { get; set; }

        public Dictionary<string, string> ProviderIds { get; set; }

        public DateTime? PremiereDate { get; set; }
        public int? ProductionYear { get; set; }

        public DateTime? DateCreated { get; set; }
        
        public List<ChannelMediaInfo> MediaSources { get; set; }
        
        public ChannelItemInfo()
        {
            MediaSources = new List<ChannelMediaInfo>();
            Genres = new List<string>();
            Studios = new List<string>();
            People = new List<PersonInfo>();
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
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

        Episode = 4,

        Song = 5
    }

    public class ChannelMediaInfo
    {
        public string Path { get; set; }

        public Dictionary<string, string> RequiredHttpHeaders { get; set; }

        public string Container { get; set; }
        public string AudioCodec { get; set; }
        public string VideoCodec { get; set; }

        public int? AudioBitrate { get; set; }
        public int? VideoBitrate { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public int? AudioChannels { get; set; }

        public ChannelMediaInfo()
        {
            RequiredHttpHeaders = new Dictionary<string, string>();
        }
    }
}
