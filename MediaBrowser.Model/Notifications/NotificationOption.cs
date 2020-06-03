#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Notifications
{
    public class NotificationOption
    {
        public NotificationOption(string type)
        {
            Type = type;

            DisabledServices = Array.Empty<string>();
            DisabledMonitorUsers = Array.Empty<string>();
            SendToUsers = Array.Empty<string>();
        }

        public string Type { get; set; }

        /// <summary>
        /// User Ids to not monitor (it's opt out)
        /// </summary>
        public string[] DisabledMonitorUsers { get; set; }

        /// <summary>
        /// User Ids to send to (if SendToUserMode == Custom)
        /// </summary>
        public string[] SendToUsers { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="NotificationOption"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the disabled services.
        /// </summary>
        /// <value>The disabled services.</value>
        public string[] DisabledServices { get; set; }

        /// <summary>
        /// Gets or sets the send to user mode.
        /// </summary>
        /// <value>The send to user mode.</value>
        public SendToUserType SendToUserMode { get; set; }
    }
}
