using MediaBrowser.Controller;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Globalization;

namespace Emby.Notifications
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
                     Type = NotificationType.GamePlayback.ToString()
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
                     Type = NotificationType.GamePlaybackStopped.ToString()
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
                }
            };

            if (!_appHost.CanSelfUpdate)
            {
                knownTypes.Add(new NotificationTypeInfo
                {
                    Type = NotificationType.ApplicationUpdateAvailable.ToString()
                });
            }

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
