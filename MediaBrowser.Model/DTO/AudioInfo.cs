using ProtoBuf;

namespace MediaBrowser.Model.DTO
{
    [ProtoContract]
    public class AudioInfo
    {
        [ProtoMember(1)]
        public int BitRate { get; set; }

        [ProtoMember(2)]
        public int Channels { get; set; }

        [ProtoMember(3)]
        public string Artist { get; set; }

        [ProtoMember(4)]
        public string Album { get; set; }

        [ProtoMember(5)]
        public string AlbumArtist { get; set; }
    }
}
