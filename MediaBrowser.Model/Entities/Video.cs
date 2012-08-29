using System.Collections.Generic;
using ProtoBuf;

namespace MediaBrowser.Model.Entities
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

    [ProtoContract]
    public class AudioStream
    {
        [ProtoMember(1)]
        public string Codec { get; set; }

        [ProtoMember(2)]
        public string Language { get; set; }

        [ProtoMember(3)]
        public int BitRate { get; set; }

        [ProtoMember(4)]
        public int Channels { get; set; }

        [ProtoMember(5)]
        public int SampleRate { get; set; }

        [ProtoMember(6)]
        public bool IsDefault { get; set; }
    }

    [ProtoContract]
    public class SubtitleStream
    {
        [ProtoMember(1)]
        public string Language { get; set; }

        [ProtoMember(2)]
        public bool IsDefault { get; set; }

        [ProtoMember(3)]
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
