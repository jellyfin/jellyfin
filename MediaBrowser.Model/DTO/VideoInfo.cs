using MediaBrowser.Model.Entities;
using ProtoBuf;

namespace MediaBrowser.Model.DTO
{
    [ProtoContract]
    public class VideoInfo
    {
        [ProtoMember(1)]
        public string Codec { get; set; }

        [ProtoMember(2)]
        public int Height { get; set; }

        [ProtoMember(3)]
        public int Width { get; set; }

        [ProtoMember(4)]
        public string ScanType { get; set; }

        [ProtoMember(5)]
        public VideoType VideoType { get; set; }

        [ProtoMember(6)]
        public SubtitleStream[] Subtitles { get; set; }

        [ProtoMember(7)]
        public AudioStream[] AudioStreams { get; set; }
    }
}
