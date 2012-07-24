
namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// This holds settings that can be personalized on a per-user, per-device basis.
    /// </summary>
    public class UserConfiguration
    {
        public int RecentItemDays { get; set; }

        public UserConfiguration()
        {
            RecentItemDays = 14;
        }
    }
}
