#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;

namespace Emby.Notifications
{
    public class CoreNotificationTypes : INotificationTypeFactory
    {
        private readonly ILocalizationManager _localization;

        public CoreNotificationTypes(ILocalizationManager localization)
        {
            _localization = localization;
        }

        public IEnumerable<NotificationTypeInfo> GetNotificationTypes()
        {
            var knownTypes = new NotificationTypeInfo[]
            {
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.ApplicationUpdateInstalled)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.InstallationFailed)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.PluginInstalled)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.PluginError)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.PluginUninstalled)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.PluginUpdateInstalled)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.ServerRestartRequired)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.TaskFailed)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.NewLibraryContent)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.AudioPlayback)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.VideoPlayback)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.AudioPlaybackStopped)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.VideoPlaybackStopped)
                },
                new NotificationTypeInfo
                {
                     Type = nameof(NotificationType.UserLockedOut)
                },
                new NotificationTypeInfo
                {
                    Type = nameof(NotificationType.ApplicationUpdateAvailable)
                }
            };

            foreach (var type in knownTypes)
            {
                Update(type);
            }

            var systemName = _localization.GetLocalizedString("System");

            return knownTypes.OrderByDescending(i => string.Equals(i.Category, systemName, StringComparison.OrdinalIgnoreCase))
                .ThenBy(i => i.Category)
                .ThenBy(i => i.Name);
        }

        private void Update(NotificationTypeInfo note)
        {
            note.Name = _localization.GetLocalizedString("NotificationOption" + note.Type);

            note.IsBasedOnUserEvent = note.Type.IndexOf("Playback", StringComparison.OrdinalIgnoreCase) != -1;

            if (note.Type.IndexOf("Playback", StringComparison.OrdinalIgnoreCase) != -1)
            {
                note.Category = _localization.GetLocalizedString("User");
            }
            else if (note.Type.IndexOf("Plugin", StringComparison.OrdinalIgnoreCase) != -1)
            {
                note.Category = _localization.GetLocalizedString("Plugin");
            }
            else if (note.Type.IndexOf("UserLockedOut", StringComparison.OrdinalIgnoreCase) != -1)
            {
                note.Category = _localization.GetLocalizedString("User");
            }
            else
            {
                note.Category = _localization.GetLocalizedString("System");
            }
        }
    }
}
