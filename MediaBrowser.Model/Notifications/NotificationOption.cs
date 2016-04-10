namespace MediaBrowser.Model.Notifications
{
    public class NotificationOption
    {
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
        /// Gets or sets the title format string.
        /// </summary>
        /// <value>The title format string.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description { get; set; }
        
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

        public NotificationOption()
        {
            DisabledServices = new string[] { };
            DisabledMonitorUsers = new string[] { };
            SendToUsers = new string[] { };
        }
    }
}