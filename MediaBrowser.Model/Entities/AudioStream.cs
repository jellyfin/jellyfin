using ProtoBuf;

namespace MediaBrowser.Model.Entities
{
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
}
