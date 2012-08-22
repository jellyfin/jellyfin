using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.DTO
{
    public class VideoInfo
    {
        public string Codec { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public string ScanType { get; set; }

        public VideoType VideoType { get; set; }

        public IEnumerable<SubtitleStream> Subtitles { get; set; }
        public IEnumerable<AudioStream> AudioStreams { get; set; }
    }
}
