using MediaBrowser.Controller;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Notifications
{
    public class CoreNotificationTypes : INotificationTypeFactory
    {
        private readonly ILocalizationManager _localization;
        private readonly IServerApplicationHost _appHost;

        public CoreNotificationTypes(ILocalizationManager localization, IServerApplicationHost appHost)
        {
            _localization = localization;
            _appHost = appHost;
        }

        public IEnumerable<NotificationTypeInfo> GetNotificationTypes()
        {
            var knownTypes = new List<NotificationTypeInfo>
            {
                new NotificationTypeInfo
                {
                     Type = NotificationType.ApplicationUpdateInstalled.ToString(),
                     DefaultDescription = "{ReleaseNotes}",
                     DefaultTitle = "A new version of Emby Server has been installed.",
                     Variables = new List<string>{"Version"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.InstallationFailed.ToString(),
                     DefaultTitle = "{Name} installation failed.",
                     Variables = new List<string>{"Name", "Version"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.PluginInstalled.ToString(),
                     DefaultTitle = "{Name} was installed.",
                     Variables = new List<string>{"Name", "Version"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.PluginError.ToString(),
                     DefaultTitle = "{Name} has encountered an error.",
                     DefaultDescription = "{ErrorMessage}",
                     Variables = new List<string>{"Name", "ErrorMessage"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.PluginUninstalled.ToString(),
                     DefaultTitle = "{Name} was uninstalled.",
                     Variables = new List<string>{"Name", "Version"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.PluginUpdateInstalled.ToString(),
                     DefaultTitle = "{Name} was updated.",
                     DefaultDescription = "{ReleaseNotes}",
                     Variables = new List<string>{"Name", "ReleaseNotes", "Version"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.ServerRestartRequired.ToString(),
                     DefaultTitle = "Please restart Emby Server to finish updating."
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.TaskFailed.ToString(),
                     DefaultTitle = "{Name} failed.",
                     DefaultDescription = "{ErrorMessage}",
                     Variables = new List<string>{"Name", "ErrorMessage"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.NewLibraryContent.ToString(),
                     DefaultTitle = "{Name} has been added to your media library.",
                     Variables = new List<string>{"Name"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.AudioPlayback.ToString(),
                     DefaultTitle = "{UserName} is playing {ItemName} on {DeviceName}.",
                     Variables = new List<string>{"UserName", "ItemName", "DeviceName", "AppName"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.GamePlayback.ToString(),
                     DefaultTitle = "{UserName} is playing {ItemName} on {DeviceName}.",
                     Variables = new List<string>{"UserName", "ItemName", "DeviceName", "AppName"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.VideoPlayback.ToString(),
                     DefaultTitle = "{UserName} is playing {ItemName} on {DeviceName}.",
                     Variables = new List<string>{"UserName", "ItemName", "DeviceName", "AppName"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.AudioPlaybackStopped.ToString(),
                     DefaultTitle = "{UserName} has finished playing {ItemName} on {DeviceName}.",
                     Variables = new List<string>{"UserName", "ItemName", "DeviceName", "AppName"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.GamePlaybackStopped.ToString(),
                     DefaultTitle = "{UserName} has finished playing {ItemName} on {DeviceName}.",
                     Variables = new List<string>{"UserName", "ItemName", "DeviceName", "AppName"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.VideoPlaybackStopped.ToString(),
                     DefaultTitle = "{UserName} has finished playing {ItemName} on {DeviceName}.",
                     Variables = new List<string>{"UserName", "ItemName", "DeviceName", "AppName"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.CameraImageUploaded.ToString(),
                     DefaultTitle = "A new camera image has been uploaded from {DeviceName}.",
                     Variables = new List<string>{"DeviceName"}
                },

                new NotificationTypeInfo
                {
                     Type = NotificationType.UserLockedOut.ToString(),
                     DefaultTitle = "{UserName} has been locked out.",
                     Variables = new List<string>{"UserName"}
                }
            };

            if (!_appHost.CanSelfUpdate)
            {
                knownTypes.Add(new NotificationTypeInfo
                {
                    Type = NotificationType.ApplicationUpdateAvailable.ToString(),
                    DefaultTitle = "A new version of Emby Server is available for download."
                });
            }

            foreach (var type in knownTypes)
            {
                Update(type);
            }

            var systemName = _localization.GetLocalizedString("CategorySystem");

            return knownTypes.OrderByDescending(i => string.Equals(i.Category, systemName, StringComparison.OrdinalIgnoreCase))
                .ThenBy(i => i.Category)
                .ThenBy(i => i.Name);
        }

        private void Update(NotificationTypeInfo note)
        {
            note.Name = _localization.GetLocalizedString("NotificationOption" + note.Type) ?? note.Type;

            note.IsBasedOnUserEvent = note.Type.IndexOf("Playback", StringComparison.OrdinalIgnoreCase) != -1;

            if (note.Type.IndexOf("Playback", StringComparison.OrdinalIgnoreCase) != -1)
            {
                note.Category = _localization.GetLocalizedString("CategoryUser");
            }
            else if (note.Type.IndexOf("Plugin", StringComparison.OrdinalIgnoreCase) != -1)
            {
                note.Category = _localization.GetLocalizedString("CategoryPlugin");
            }
            else if (note.Type.IndexOf("CameraImageUploaded", StringComparison.OrdinalIgnoreCase) != -1)
            {
                note.Category = _localization.GetLocalizedString("CategorySync");
            }
            else if (note.Type.IndexOf("UserLockedOut", StringComparison.OrdinalIgnoreCase) != -1)
            {
                note.Category = _localization.GetLocalizedString("CategoryUser");
            }
            else
            {
                note.Category = _localization.GetLocalizedString("CategorySystem");
            }
        }
    }
}
