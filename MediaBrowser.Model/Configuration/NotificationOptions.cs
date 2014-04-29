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
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption
                {
                    Type = NotificationType.ServerRestartRequired.ToString(),
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption
                {
                    Type = NotificationType.ApplicationUpdateAvailable.ToString(),
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption
                {
                    Type = NotificationType.ApplicationUpdateInstalled.ToString(),
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption
                {
                    Type = NotificationType.PluginUpdateInstalled.ToString(),
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption
                {
                    Type = NotificationType.PluginUninstalled.ToString(),
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption
                {
                    Type = NotificationType.InstallationFailed.ToString(),
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption
                {
                    Type = NotificationType.PluginInstalled.ToString(),
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption
                {
                    Type = NotificationType.PluginError.ToString(),
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
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

        public bool IsEnabledToSendToUser(string type, string userId, UserConfiguration userConfig)
        {
            var opt = GetOptions(type);

            if (opt != null && opt.Enabled)
            {
                if (opt.SendToUserMode == SendToUserType.All)
                {
                    return true;
                }

                if (opt.SendToUserMode == SendToUserType.Admins && userConfig.IsAdministrator)
                {
                    return true;
                }

                return opt.SendToUsers.Contains(userId, StringComparer.OrdinalIgnoreCase);
            }

            return false;
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

    public enum NotificationType
    {
        ApplicationUpdateAvailable,
        ApplicationUpdateInstalled,
        AudioPlayback,
        GamePlayback,
        InstallationFailed,
        PluginError,
        PluginInstalled,
        PluginUpdateInstalled,
        PluginUninstalled,
        NewLibraryContent,
        ServerRestartRequired,
        TaskFailed,
        VideoPlayback
    }

    public enum SendToUserType
    {
        All = 0,
        Admins = 1,
        Custom = 2
    }
}
