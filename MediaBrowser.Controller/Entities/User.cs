using System;

namespace MediaBrowser.Controller.Entities
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
    }
}
