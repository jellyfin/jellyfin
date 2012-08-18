using System;

namespace MediaBrowser.Model.Entities
{
    public class UserItemData
    {
        public UserItemRating Rating { get; set; }

        public long PlaybackPositionTicks { get; set; }

        public int PlayCount { get; set; }
    }

    public enum UserItemRating
    {
        Likes,
        Dislikes,
        Favorite
    }
}
