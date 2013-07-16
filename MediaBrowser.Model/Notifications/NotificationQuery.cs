
namespace MediaBrowser.Model.Notifications
{
    public class NotificationQuery
    {
        public string UserId { get; set; }

        public bool? IsRead { get; set; }

        public int? StartIndex { get; set; }

        public int? Limit { get; set; }
    }
}
