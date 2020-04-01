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
                     Type = NotificationType.ApplicationUpdateInstalled.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.InstallationFailed.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.PluginInstalled.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.PluginError.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.PluginUninstalled.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.PluginUpdateInstalled.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.ServerRestartRequired.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.TaskFailed.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.NewLibraryContent.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.AudioPlayback.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.VideoPlayback.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.AudioPlaybackStopped.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.VideoPlaybackStopped.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.CameraImageUploaded.ToString()
                },
                new NotificationTypeInfo
                {
                     Type = NotificationType.UserLockedOut.ToString()
                },
                new NotificationTypeInfo
                {
                    Type = NotificationType.ApplicationUpdateAvailable.ToString()
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
            note.Name = _localization.GetLocalizedString("NotificationOption" + note.Type) ?? note.Type;

            note.IsBasedOnUserEvent = note.Type.IndexOf("Playback", StringComparison.OrdinalIgnoreCase) != -1;

            if (note.Type.IndexOf("Playback", StringComparison.OrdinalIgnoreCase) != -1)
            {
                note.Category = _localization.GetLocalizedString("User");
            }
            else if (note.Type.IndexOf("Plugin", StringComparison.OrdinalIgnoreCase) != -1)
            {
                note.Category = _localization.GetLocalizedString("Plugin");
            }
            else if (note.Type.IndexOf("CameraImageUploaded", StringComparison.OrdinalIgnoreCase) != -1)
            {
                note.Category = _localization.GetLocalizedString("Sync");
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
