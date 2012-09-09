using System;

namespace MediaBrowser.Model.Entities
{
    public class User : BaseEntity
    {
        public string Password { get; set; }
        
        public string MaxParentalRating { get; set; }

        public int RecentItemDays { get; set; }

        public User()
        {
            RecentItemDays = 14;
        }

        public DateTime? LastLoginDate { get; set; }
        public DateTime? LastActivityDate { get; set; }

        /// <summary>
        /// This allows the user to configure how they want to rate items
        /// </summary>
        public ItemRatingMode ItemRatingMode { get; set; }
    }

    public enum ItemRatingMode
    {
        LikeOrDislike,
        Numeric
    }
}
