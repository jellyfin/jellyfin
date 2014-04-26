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
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Tasks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            _installationManager.PackageInstallationCompleted += _installationManager_PackageInstallationCompleted;
            _installationManager.PackageInstallationFailed += _installationManager_PackageInstallationFailed;
            _installationManager.PluginUninstalled += _installationManager_PluginUninstalled;

            _taskManager.TaskCompleted += _taskManager_TaskCompleted;

            _userManager.UserCreated += _userManager_UserCreated;
            _libraryManager.ItemAdded += _libraryManager_ItemAdded;
            _sessionManager.PlaybackStart += _sessionManager_PlaybackStart;
            _appHost.HasPendingRestartChanged += _appHost_HasPendingRestartChanged;
            _appHost.HasUpdateAvailableChanged += _appHost_HasUpdateAvailableChanged;
        }

        async void _appHost_HasUpdateAvailableChanged(object sender, EventArgs e)
        {
            // This notification is for users who can't auto-update (aka running as service)
            if (!_appHost.HasUpdateAvailable || _appHost.CanSelfUpdate || !_config.Configuration.NotificationOptions.SendOnUpdates)
            {
                return;
            }

            var userIds = _userManager
              .Users
              .Where(i => i.Configuration.IsAdministrator)
              .Select(i => i.Id.ToString("N"))
              .ToList();

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                Name = "A new version of Media Browser is available.",
                Description = "Please see mediabrowser3.com for details."
            };

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _appHost_HasPendingRestartChanged(object sender, EventArgs e)
        {
            if (!_appHost.HasPendingRestart || !_config.Configuration.NotificationOptions.SendOnUpdates)
            {
                return;
            }

            var userIds = _userManager
              .Users
              .Where(i => i.Configuration.IsAdministrator)
              .Select(i => i.Id.ToString("N"))
              .ToList();

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                Name = "Please restart Media Browser to finish updating"
            };

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _sessionManager_PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            if (!NotifyOnPlayback(e.MediaInfo.MediaType))
            {
                return;
            }

            var userIds = _userManager
              .Users
              .Where(i => i.Configuration.IsAdministrator)
              .Select(i => i.Id.ToString("N"))
              .ToList();

            var item = e.MediaInfo;

            var msgName = "playing " + item.Name;

            var user = e.Users.FirstOrDefault();

            if (user != null)
            {
                msgName = user.Name + " " + msgName;
            }

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                Name = msgName
            };

            await SendNotification(notification).ConfigureAwait(false);
        }

        private bool NotifyOnPlayback(string mediaType)
        {
            if (string.Equals(mediaType, MediaType.Audio, StringComparison.OrdinalIgnoreCase))
            {
                return _config.Configuration.NotificationOptions.SendOnAudioPlayback;
            }
            if (string.Equals(mediaType, MediaType.Game, StringComparison.OrdinalIgnoreCase))
            {
                return _config.Configuration.NotificationOptions.SendOnGamePlayback;
            }
            if (string.Equals(mediaType, MediaType.Video, StringComparison.OrdinalIgnoreCase))
            {
                return _config.Configuration.NotificationOptions.SendOnVideoPlayback;
            }

            return false;
        }

        async void _libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            if (_config.Configuration.NotificationOptions.SendOnNewLibraryContent &&
                e.Item.LocationType == LocationType.FileSystem)
            {
                var userIds = _userManager
                  .Users
                  .Where(i => i.Configuration.IsAdministrator)
                  .Select(i => i.Id.ToString("N"))
                  .ToList();

                var item = e.Item;

                var notification = new NotificationRequest
                {
                    UserIds = userIds,
                    Name = item.Name + " added to library."
                };

                await SendNotification(notification).ConfigureAwait(false);
            }
        }

        async void _userManager_UserCreated(object sender, GenericEventArgs<User> e)
        {
            var userIds = _userManager
              .Users
              .Select(i => i.Id.ToString("N"))
              .ToList();

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                Name = "Welcome to Media Browser!",
                Description = "Check back here for more notifications."
            };

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _taskManager_TaskCompleted(object sender, GenericEventArgs<TaskResult> e)
        {
            var result = e.Argument;

            if (result.Status == TaskCompletionStatus.Failed &&
                _config.Configuration.NotificationOptions.SendOnFailedTasks)
            {
                var userIds = _userManager
                  .Users
                  .Where(i => i.Configuration.IsAdministrator)
                  .Select(i => i.Id.ToString("N"))
                  .ToList();

                var notification = new NotificationRequest
                {
                    UserIds = userIds,
                    Name = result.Name + " failed",
                    Description = result.ErrorMessage,
                    Level = NotificationLevel.Error
                };

                await SendNotification(notification).ConfigureAwait(false);
            }
        }

        async void _installationManager_PluginUninstalled(object sender, GenericEventArgs<IPlugin> e)
        {
            var plugin = e.Argument;

            var userIds = _userManager
              .Users
              .Where(i => i.Configuration.IsAdministrator)
              .Select(i => i.Id.ToString("N"))
              .ToList();

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                Name = plugin.Name + " has been uninstalled"
            };

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _installationManager_PackageInstallationCompleted(object sender, InstallationEventArgs e)
        {
            if (!_config.Configuration.NotificationOptions.SendOnUpdates)
            {
                return;
            }

            var installationInfo = e.InstallationInfo;

            var userIds = _userManager
              .Users
              .Where(i => i.Configuration.IsAdministrator)
              .Select(i => i.Id.ToString("N"))
              .ToList();

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                Name = installationInfo.Name + " " + installationInfo.Version + " was installed",
                Description = e.PackageVersionInfo.description
            };

            await SendNotification(notification).ConfigureAwait(false);
        }

        async void _installationManager_PackageInstallationFailed(object sender, InstallationFailedEventArgs e)
        {
            var installationInfo = e.InstallationInfo;

            var userIds = _userManager
                .Users
                .Where(i => i.Configuration.IsAdministrator)
                .Select(i => i.Id.ToString("N"))
                .ToList();

            var notification = new NotificationRequest
            {
                UserIds = userIds,
                Level = NotificationLevel.Error,
                Name = installationInfo.Name + " " + installationInfo.Version + " installation failed",
                Description = e.Exception.Message
            };

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
            _installationManager.PackageInstallationCompleted -= _installationManager_PackageInstallationCompleted;
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
