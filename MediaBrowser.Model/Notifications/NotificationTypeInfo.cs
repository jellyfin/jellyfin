using System.Collections.Generic;

namespace MediaBrowser.Model.Notifications
{
    public class NotificationTypeInfo
    {
        public string Type { get; set; }

        public string Name { get; set; }

        public bool Enabled { get; set; }

        public string Category { get; set; }

        public bool IsBasedOnUserEvent { get; set; }

        public string DefaultTitle { get; set; }

        public string DefaultDescription { get; set; }
        
        public List<string> Variables { get; set; }

        public NotificationTypeInfo()
        {
            Variables = new List<string>();
        }
    }
}