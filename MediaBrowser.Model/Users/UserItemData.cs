using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Users
{
    public class UserItemData
    {
        public UserItemRating Rating { get; set; }

        public PlaybackStatus PlaybackStatus { get; set; }
    }

    public enum UserItemRating
    {
        Likes,
        Dislikes,
        Favorite
    }
}
