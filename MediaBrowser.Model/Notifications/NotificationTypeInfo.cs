#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Notifications
{
    public class NotificationTypeInfo
    {
        public string Type { get; set; }

        public string Name { get; set; }

        public bool Enabled { get; set; }

        public string Category { get; set; }

        public bool IsBasedOnUserEvent { get; set; }
    }
}
