using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Model.Notifications
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
                },
                new NotificationOption
                {
                    Type = NotificationType.UserLockedOut.ToString(),
                    Enabled = true,
                    SendToUserMode = SendToUserType.Admins
                }
            };
        }

        public NotificationOption GetOptions(string type)
        {
            foreach (NotificationOption i in Options)
            {
                if (StringHelper.EqualsIgnoreCase(type, i.Type)) return i;
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

            return opt == null ||
                   !ListHelper.ContainsIgnoreCase(opt.DisabledServices, service);
        }

        public bool IsEnabledToMonitorUser(string type, string userId)
        {
            NotificationOption opt = GetOptions(type);

            return opt != null && opt.Enabled &&
                   !ListHelper.ContainsIgnoreCase(opt.DisabledMonitorUsers, userId);
        }

        public bool IsEnabledToSendToUser(string type, string userId, UserPolicy userPolicy)
        {
            NotificationOption opt = GetOptions(type);

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

                return ListHelper.ContainsIgnoreCase(opt.SendToUsers, userId);
            }

            return false;
        }
    }
}
