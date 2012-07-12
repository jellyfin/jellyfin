using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Entities
{
    public class Video : BaseItem
    {
        public VideoType VideoType { get; set; }

        private IEnumerable<string> _Subtitles = new string[] { };
        public IEnumerable<string> Subtitles { get { return _Subtitles; } set { _Subtitles = value; } }

        private IEnumerable<AudioStream> _AudioStreams = new AudioStream[] { };
        public IEnumerable<AudioStream> AudioStreams { get { return _AudioStreams; } set { _AudioStreams = value; } }

        public int Height { get; set; }
        public int Width { get; set; }
        public string ScanType { get; set; }
        public string FrameRate { get; set; }
        public int VideoBitRate { get; set; }
        public string VideoCodec { get; set; }
    }

    public class AudioStream
    {
        public string AudioFormat { get; set; }
        public string AudioProfile { get; set; }
        public string Language { get; set; }
        public int BitRate { get; set; }
        public int Channels { get; set; }
    }

    public enum VideoType
    {
        VideoFile = 1,
        DVD = 2,
        BluRay = 3
    }
}
