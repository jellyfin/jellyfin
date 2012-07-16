using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Model.Users
{
    public class UserItemData
    {
        public UserItemRating Rating { get; set; }

        public TimeSpan PlaybackPosition { get; set; }

        public int PlayCount { get; set; }
    }

    public enum UserItemRating
    {
        Likes,
        Dislikes,
        Favorite
    }
}
