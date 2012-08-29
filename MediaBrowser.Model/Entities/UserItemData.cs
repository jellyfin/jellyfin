using System;
using ProtoBuf;

namespace MediaBrowser.Model.Entities
{
    [ProtoContract]
    public class UserItemData
    {
        [ProtoMember(1)]
        public UserItemRating Rating { get; set; }

        [ProtoMember(2)]
        public long PlaybackPositionTicks { get; set; }

        [ProtoMember(3)]
        public int PlayCount { get; set; }
    }

    public enum UserItemRating
    {
        Likes,
        Dislikes,
        Favorite
    }
}
