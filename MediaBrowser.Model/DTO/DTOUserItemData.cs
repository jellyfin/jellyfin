using ProtoBuf;

namespace MediaBrowser.Model.DTO
{
    [ProtoContract]
    public class DtoUserItemData
    {
        [ProtoMember(1)]
        public float? Rating { get; set; }

        [ProtoMember(2)]
        public long PlaybackPositionTicks { get; set; }

        [ProtoMember(3)]
        public int PlayCount { get; set; }

        [ProtoMember(4)]
        public bool IsFavorite { get; set; }

        [ProtoMember(5)]
        public bool? Likes { get; set; }
    }
}
