using MediaBrowser.Model.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public class Video : BaseItem
    {
        public VideoType VideoType { get; set; }

        public List<SubtitleStream> Subtitles { get; set; }
        public List<AudioStream> AudioStreams { get; set; }

        public int Height { get; set; }
        public int Width { get; set; }
        public string ScanType { get; set; }
        public float FrameRate { get; set; }
        public int BitRate { get; set; }
        public string Codec { get; set; }
    }
}
