#nullable disable
#pragma warning disable CS1591

using System;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;

namespace MediaBrowser.Model.Notifications
{
    public class NotificationOptions
    {
        public NotificationOptions()
        {
            Options = new[]
            {
                new NotificationOption(NotificationType.TaskFailed.ToString())
                {
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption(NotificationType.ServerRestartRequired.ToString())
                {
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption(NotificationType.ApplicationUpdateAvailable.ToString())
                {
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption(NotificationType.ApplicationUpdateInstalled.ToString())
                {
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption(NotificationType.PluginUpdateInstalled.ToString())
                {
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption(NotificationType.PluginUninstalled.ToString())
                {
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption(NotificationType.InstallationFailed.ToString())
                {
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption(NotificationType.PluginInstalled.ToString())
                {
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption(NotificationType.PluginError.ToString())
                {
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                },
                new NotificationOption(NotificationType.UserLockedOut.ToString())
                {
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                }
            };
        }

        public NotificationOption[] Options { get; set; }

        public NotificationOption GetOptions(string type)
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
            NotificationOption opt = GetOptions(type);

            return opt != null && opt.Enabled;
        }

        public bool IsServiceEnabled(string service, string notificationType)
        {
            NotificationOption opt = GetOptions(notificationType);

            return opt == null
                   || !opt.DisabledServices.Contains(service, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsEnabledToMonitorUser(string type, Guid userId)
        {
            NotificationOption opt = GetOptions(type);

            return opt != null
                   && opt.Enabled
                   && !opt.DisabledMonitorUsers.Contains(userId.ToString("N"), StringComparison.OrdinalIgnoreCase);
        }

        public bool IsEnabledToSendToUser(string type, string userId, User user)
        {
            NotificationOption opt = GetOptions(type);

            if (opt != null && opt.Enabled)
            {
                if (opt.SendToUserMode == SendToUserType.All)
                {
                    return true;
                }

                if (opt.SendToUserMode == SendToUserType.Admins && user.HasPermission(PermissionKind.IsAdministrator))
                {
                    return true;
                }

                return opt.SendToUsers.Contains(userId, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
