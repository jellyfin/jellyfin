#pragma warning disable CS1591
#pragma warning disable CA1819 // Properties should not return arrays

using System;
using System.Globalization;
using System.Linq;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Model.Notifications
{
    public class NotificationOptions
    {
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
                },
                new NotificationOption
                {
                    Type = NotificationType.UserLockedOut.ToString(),
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                }
            };
        }

        public NotificationOption[] Options { get; set; }

        public NotificationOption GetOptionsOfType(string type)
        {
            foreach (NotificationOption i in Options)
            {
                if (string.Equals(type, i.Type, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return null;
        }

        public bool IsEnabled(string type)
        {
            NotificationOption opt = GetOptionsOfType(type);

            return opt != null && opt.Enabled;
        }

        public bool IsServiceEnabled(string service, string notificationType)
        {
            NotificationOption opt = GetOptionsOfType(notificationType);

            return opt == null ||
                   !opt.DisabledServices.Contains(service, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsEnabledToMonitorUser(string type, Guid userId)
        {
            NotificationOption opt = GetOptionsOfType(type);

            return opt != null && opt.Enabled &&
                   !opt.DisabledMonitorUsers.Contains(userId.ToString(string.Empty, CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase);
        }

        public bool IsEnabledToSendToUser(string type, string userId, UserPolicy userPolicy)
        {
            NotificationOption opt = GetOptionsOfType(type);

            if (opt != null && opt.Enabled)
            {
                if (opt.SendToUserMode == SendToUserType.All)
                {
                    return true;
                }

                if (opt.SendToUserMode == SendToUserType.Admins && userPolicy.IsAdministrator)
                {
                    return true;
                }

                return opt.SendToUsers.Contains(userId, StringComparer.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
