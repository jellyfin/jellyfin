
namespace MediaBrowser.Model.Entities
{
    public class User : BaseEntity
    {
        public string MaxParentalRating { get; set; }

        public int RecentItemDays { get; set; }

        public User()
        {
            RecentItemDays = 14;
        }
    }
}
