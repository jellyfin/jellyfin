using System;
using System.Linq;

namespace MediaBrowser.Model.Configuration
{
    public class NotificationOptions
    {
        public NotificationOption[] Options { get; set; }

        public NotificationOptions()
        {
            Options = new[]
            {
                new NotificationOption
                {
                    Type = NotificationType.TaskFailed.ToString(),
                    Enabled = true
                },
                new NotificationOption
                {
                    Type = NotificationType.ServerRestartRequired.ToString(),
                    Enabled = true
                },
                new NotificationOption
                {
                    Type = NotificationType.ApplicationUpdateAvailable.ToString(),
                    Enabled = true
                },
                new NotificationOption
                {
                    Type = NotificationType.ApplicationUpdateInstalled.ToString(),
                    Enabled = true
                },
                new NotificationOption
                {
                    Type = NotificationType.PluginUpdateInstalled.ToString(),
                    Enabled = true
                },
                new NotificationOption
                {
                    Type = NotificationType.PluginUninstalled.ToString(),
                    Enabled = true
                },
                new NotificationOption
                {
                    Type = NotificationType.InstallationFailed.ToString(),
                    Enabled = true
                },
                new NotificationOption
                {
                    Type = NotificationType.PluginInstalled.ToString(),
                    Enabled = true
                }
            };
        }

        public NotificationOption GetOptions(string type)
        {
            return Options.FirstOrDefault(i => string.Equals(type, i.Type, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsEnabled(string type)
        {
            var opt = GetOptions(type);

            return opt != null && opt.Enabled;
        }

        public bool IsServiceEnabled(string service, string notificationType)
        {
            var opt = GetOptions(notificationType);

            return opt == null ||
                   !opt.DisabledServices.Contains(service, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsEnabledToMonitorUser(string type, string userId)
        {
            var opt = GetOptions(type);

            return opt != null && opt.Enabled &&
                   !opt.DisabledMonitorUsers.Contains(userId, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsEnabledToSendToUser(string type, string userId)
        {
            var opt = GetOptions(type);

            return opt != null && opt.Enabled &&
                   !opt.DisabledSendToUsers.Contains(userId, StringComparer.OrdinalIgnoreCase);
        }
    }

    public class NotificationOption
    {
        public string Type { get; set; }

        /// <summary>
        /// User Ids to not monitor (it's opt out)
        /// </summary>
        public string[] DisabledMonitorUsers { get; set; }

        /// <summary>
        /// User Ids to not send to (it's opt out)
        /// </summary>
        public string[] DisabledSendToUsers { get; set; }

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
        /// Gets or sets the disabled services.
        /// </summary>
        /// <value>The disabled services.</value>
        public string[] DisabledServices { get; set; }
        
        public NotificationOption()
        {
            DisabledServices = new string[] { };
            DisabledMonitorUsers = new string[] { };
            DisabledSendToUsers = new string[] { };
        }
    }

    public enum NotificationType
    {
        TaskFailed,
        InstallationFailed,
        NewLibraryContent,
        ServerRestartRequired,
        ApplicationUpdateAvailable,
        ApplicationUpdateInstalled,
        PluginInstalled,
        PluginUpdateInstalled,
        PluginUninstalled,
        AudioPlayback,
        GamePlayback,
        VideoPlayback
    }
}
