using ProtoBuf;

namespace MediaBrowser.Model.Entities
{
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
}
