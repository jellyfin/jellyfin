using MediaBrowser.Common.Events;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Server.Implementations.EntryPoints.Notifications
{
    /// <summary>
    /// Creates notifications for various system events
    /// </summary>
    public class Notifications : IServerEntryPoint
    {
        private readonly IInstallationManager _installationManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        private readonly ITaskManager _taskManager;
        private readonly INotificationManager _notificationManager;

        private readonly IServerConfigurationManager _config;
        private readonly ILibraryManager _libraryManager;
        private readonly ISessionManager _sessionManager;
        private readonly IServerApplicationHost _appHost;

        public Notifications(IInstallationManager installationManager, IUserManager userManager, ILogger logger, ITaskManager taskManager, INotificationManager notificationManager, IServerConfigurationManager config, ILibraryManager libraryManager, ISessionManager sessionManager, IServerApplicationHost appHost)
        {
            _installationManager = installationManager;
            _userManager = userManager;
            _logger = logger;
            _taskManager = taskManager;
            _notificationManager = notificationManager;
            _config = config;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _appHost = appHost;
        }

        public void Run()
        {
            _installationManager.PluginInstalled += _installationManager_PluginInstalled;
            _installationManager.PluginUpdated += _installationManager_PluginUpdated;
            _installationManager.PackageInstallationFailed += _installationManager_PackageInstallationFailed;
            _installationManager.PluginUninstalled += _installationManager_PluginUninstalled;

            _taskManager.TaskCompleted += _taskManager_TaskCompleted;

            _userManager.UserCreated += _userManager_UserCreated;
            _libraryManager.ItemAdded += _libraryManager_ItemAdded;
            _sessionManager.PlaybackStart += _sessionManager_PlaybackStart;
            _appHost.HasPendingRestartChanged += _appHost_HasPendingRestartChanged;
            _appHost.HasUpdateAvailableChanged += _appHost_HasUpdateAvailableChanged;
        }

        async void _installationManager_PluginUpdated(object sender, GenericEventArgs<Tuple<IPlugin, PackageVersionInfo>> e)
        {
            var type = NotificationType.PluginUpdateInstalled.ToString();

            var installationInfo = e.Argument.Item1;

            var userIds = _userManager
              .Users
              .Where(i => i.Configuration.IsAdministrator && _config.Configuration.NotificationOptions.IsEnabledToSendToUser(type, i.Id.ToString("N")))
              .Select(i => i.Id.ToString("N"))
              .ToList();

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                Description = installationInfo.Description,
                NotificationType = type
            };

            notification.Variables["Name"] = installationInfo.Name;
            notification.Variables["Version"] = installationInfo.Version.ToString();

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _installationManager_PluginInstalled(object sender, GenericEventArgs<PackageVersionInfo> e)
        {
            var type = NotificationType.PluginInstalled.ToString();

            var installationInfo = e.Argument;

            var userIds = _userManager
              .Users
              .Where(i => i.Configuration.IsAdministrator && _config.Configuration.NotificationOptions.IsEnabledToSendToUser(type, i.Id.ToString("N")))
              .Select(i => i.Id.ToString("N"))
              .ToList();

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                Description = installationInfo.description,
                NotificationType = type
            };

            notification.Variables["Name"] = installationInfo.name;
            notification.Variables["Version"] = installationInfo.versionStr;

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _appHost_HasUpdateAvailableChanged(object sender, EventArgs e)
        {
            // This notification is for users who can't auto-update (aka running as service)
            if (!_appHost.HasUpdateAvailable || _appHost.CanSelfUpdate)
            {
                return;
            }

            var type = NotificationType.ApplicationUpdateAvailable.ToString();

            var userIds = _userManager
              .Users
              .Where(i => i.Configuration.IsAdministrator && _config.Configuration.NotificationOptions.IsEnabledToSendToUser(type, i.Id.ToString("N")))
              .Select(i => i.Id.ToString("N"))
              .ToList();

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                Description = "Please see mediabrowser3.com for details.",
                NotificationType = type
            };

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _appHost_HasPendingRestartChanged(object sender, EventArgs e)
        {
            if (!_appHost.HasPendingRestart)
            {
                return;
            }

            var type = NotificationType.ServerRestartRequired.ToString();

            var userIds = _userManager
              .Users
              .Where(i => i.Configuration.IsAdministrator && _config.Configuration.NotificationOptions.IsEnabledToSendToUser(type, i.Id.ToString("N")))
              .Select(i => i.Id.ToString("N"))
              .ToList();

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                NotificationType = type
            };

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _sessionManager_PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            var user = e.Users.FirstOrDefault();

            var userIds = _userManager
              .Users
              .Where(i => NotifyOnPlayback(e.MediaInfo.MediaType, user, i))
              .Select(i => i.Id.ToString("N"))
              .ToList();

            var item = e.MediaInfo;

            var notification = new NotificationRequest
            {
                UserIds = userIds
            };

            notification.Variables["ItemName"] = item.Name;
            notification.Variables["UserName"] = user == null ? "Unknown user" : user.Name;
            notification.Variables["AppName"] = e.ClientName;
            notification.Variables["DeviceName"] = e.DeviceName;

            await SendNotification(notification).ConfigureAwait(false);
        }

        private bool NotifyOnPlayback(string mediaType, User playingUser, User notifiedUser)
        {
            if (string.Equals(mediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
            {
                var type = NotificationType.AudioPlayback.ToString();

                if (playingUser != null)
                {
                    if (!_config.Configuration.NotificationOptions.IsEnabledToMonitorUser(
                            type, playingUser.Id.ToString("N")))
                    {
                        return false;
                    }

                    if (playingUser.Id == notifiedUser.Id)
                    {
                        return false;
                    }
                }

                return _config.Configuration.NotificationOptions.IsEnabledToSendToUser(type, notifiedUser.Id.ToString("N"));
            }
            if (string.Equals(mediaType, MediaType.Game, StringComparison.OrdinalIgnoreCase))
            {
                var type = NotificationType.GamePlayback.ToString();

                if (playingUser != null)
                {
                    if (!_config.Configuration.NotificationOptions.IsEnabledToMonitorUser(
                            type, playingUser.Id.ToString("N")))
                    {
                        return false;
                    }

                    if (playingUser.Id == notifiedUser.Id)
                    {
                        return false;
                    }
                }

                return _config.Configuration.NotificationOptions.IsEnabledToSendToUser(type, notifiedUser.Id.ToString("N"));
            }
            if (string.Equals(mediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                var type = NotificationType.VideoPlayback.ToString();

                if (playingUser != null)
                {
                    if (!_config.Configuration.NotificationOptions.IsEnabledToMonitorUser(
                            type, playingUser.Id.ToString("N")))
                    {
                        return false;
                    }

                    if (playingUser.Id == notifiedUser.Id)
                    {
                        return false;
                    }
                }

                return _config.Configuration.NotificationOptions.IsEnabledToSendToUser(type, notifiedUser.Id.ToString("N"));
            }

            return false;
        }

        async void _libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            if (e.Item.LocationType == LocationType.FileSystem)
            {
                var type = NotificationType.NewLibraryContent.ToString();

                var userIds = _userManager
                  .Users
                  .Where(i => _config.Configuration.NotificationOptions.IsEnabledToSendToUser(type, i.Id.ToString("N")))
                  .Select(i => i.Id.ToString("N"))
                  .ToList();

                var item = e.Item;

                var notification = new NotificationRequest
                {
                    UserIds = userIds,
                    NotificationType = type
                };

                notification.Variables["Name"] = item.Name;
                
                await SendNotification(notification).ConfigureAwait(false);
            }
        }

        async void _userManager_UserCreated(object sender, GenericEventArgs<User> e)
        {
            var notification = new NotificationRequest
            {
                UserIds = new List<string> { e.Argument.Id.ToString("N") },
                Name = "Welcome to Media Browser!",
                Description = "Check back here for more notifications."
            };

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _taskManager_TaskCompleted(object sender, GenericEventArgs<TaskResult> e)
        {
            var result = e.Argument;

            if (result.Status == TaskCompletionStatus.Failed)
            {
                var type = NotificationType.TaskFailed.ToString();

                var userIds = _userManager
                  .Users
                  .Where(i => i.Configuration.IsAdministrator && _config.Configuration.NotificationOptions.IsEnabledToSendToUser(type, i.Id.ToString("N")))
                  .Select(i => i.Id.ToString("N"))
                  .ToList();

                var notification = new NotificationRequest
                {
                    UserIds = userIds,
                    Description = result.ErrorMessage,
                    Level = NotificationLevel.Error,
                    NotificationType = type
                };

                notification.Variables["Name"] = e.Argument.Name;

                await SendNotification(notification).ConfigureAwait(false);
            }
        }

        async void _installationManager_PluginUninstalled(object sender, GenericEventArgs<IPlugin> e)
        {
            var type = NotificationType.PluginUninstalled.ToString();
            
            var plugin = e.Argument;

            var userIds = _userManager
              .Users
              .Where(i => i.Configuration.IsAdministrator && _config.Configuration.NotificationOptions.IsEnabledToSendToUser(type, i.Id.ToString("N")))
              .Select(i => i.Id.ToString("N"))
              .ToList();

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                NotificationType = type
            };

            notification.Variables["Name"] = plugin.Name;
            notification.Variables["Version"] = plugin.Version.ToString();
            
            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _installationManager_PackageInstallationFailed(object sender, InstallationFailedEventArgs e)
        {
            var installationInfo = e.InstallationInfo;

            var type = NotificationType.InstallationFailed.ToString();

            var userIds = _userManager
                .Users
                .Where(i => i.Configuration.IsAdministrator && _config.Configuration.NotificationOptions.IsEnabledToSendToUser(type, i.Id.ToString("N")))
                .Select(i => i.Id.ToString("N"))
                .ToList();

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                Level = NotificationLevel.Error,
                Description = e.Exception.Message,
                NotificationType = type
            };

            notification.Variables["Name"] = installationInfo.Name;
            notification.Variables["Version"] = installationInfo.Version;

            await SendNotification(notification).ConfigureAwait(false);
        }

        private async Task SendNotification(NotificationRequest notification)
        {
            try
            {
                await _notificationManager.SendNotification(notification, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending notification", ex);
            }
        }

        public void Dispose()
        {
            _installationManager.PluginInstalled -= _installationManager_PluginInstalled;
            _installationManager.PluginUpdated -= _installationManager_PluginUpdated;
            _installationManager.PackageInstallationFailed -= _installationManager_PackageInstallationFailed;
            _installationManager.PluginUninstalled -= _installationManager_PluginUninstalled;

            _taskManager.TaskCompleted -= _taskManager_TaskCompleted;

            _userManager.UserCreated -= _userManager_UserCreated;
            _libraryManager.ItemAdded -= _libraryManager_ItemAdded;
            _sessionManager.PlaybackStart -= _sessionManager_PlaybackStart;

            _appHost.HasPendingRestartChanged -= _appHost_HasPendingRestartChanged;
            _appHost.HasUpdateAvailableChanged -= _appHost_HasUpdateAvailableChanged;
        }
    }
}
