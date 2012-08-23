using System.Collections.Generic;

namespace MediaBrowser.Model.Entities
{
    public class Video : BaseItem
    {
        public VideoType VideoType { get; set; }

        public IEnumerable<SubtitleStream> Subtitles { get; set; }
        public IEnumerable<AudioStream> AudioStreams { get; set; }

        public int Height { get; set; }
        public int Width { get; set; }
        public string ScanType { get; set; }
        public float FrameRate { get; set; }
        public int BitRate { get; set; }
        public string Codec { get; set; }
    }

    public class AudioStream
    {
        public string Codec { get; set; }
        public string Language { get; set; }
        public int BitRate { get; set; }
        public int Channels { get; set; }
        public int SampleRate { get; set; }
        public bool IsDefault { get; set; }
    }

    public class SubtitleStream
    {
        public string Language { get; set; }
        public bool IsDefault { get; set; }
        public bool IsForced { get; set; }
    }

    public enum VideoType
    {
        VideoFile,
        Iso,
        DVD,
        BluRay
    }
}
