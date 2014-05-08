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
            NotificationOption opt = GetOptions(type);

            return opt != null && opt.Enabled;
        }

        public bool IsServiceEnabled(string service, string notificationType)
        {
            NotificationOption opt = GetOptions(notificationType);

            return opt == null ||
                   !opt.DisabledServices.Contains(service, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsEnabledToMonitorUser(string type, string userId)
        {
            NotificationOption opt = GetOptions(type);

            return opt != null && opt.Enabled &&
                   !opt.DisabledMonitorUsers.Contains(userId, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsEnabledToSendToUser(string type, string userId, UserConfiguration userConfig)
        {
            NotificationOption opt = GetOptions(type);

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
}
