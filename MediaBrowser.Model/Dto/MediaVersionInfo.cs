using MediaBrowser.Model.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dto
{
    public class MediaVersionInfo
    {
        public string Id { get; set; }

        public string Path { get; set; }

        public LocationType LocationType { get; set; }
        
        public string Name { get; set; }
        
        public long? RunTimeTicks { get; set; }

        public VideoType? VideoType { get; set; }

        public IsoType? IsoType { get; set; }

        public Video3DFormat? Video3DFormat { get; set; }
        
        public List<MediaStream> MediaStreams { get; set; }

        public List<ChapterInfoDto> Chapters { get; set; }
    }
}
