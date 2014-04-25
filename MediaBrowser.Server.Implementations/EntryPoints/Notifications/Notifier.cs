using MediaBrowser.Common.Events;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Tasks;
using System;
using System.Linq;
using System.Threading;

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

        public Notifications(IInstallationManager installationManager, IUserManager userManager, ILogger logger, ITaskManager taskManager, INotificationManager notificationManager)
        {
            _installationManager = installationManager;
            _userManager = userManager;
            _logger = logger;
            _taskManager = taskManager;
            _notificationManager = notificationManager;
        }

        public void Run()
        {
            _installationManager.PackageInstallationCompleted += _installationManager_PackageInstallationCompleted;
            _installationManager.PackageInstallationFailed += _installationManager_PackageInstallationFailed;
            _installationManager.PluginUninstalled += _installationManager_PluginUninstalled;

            _taskManager.TaskCompleted += _taskManager_TaskCompleted;

            _userManager.UserCreated += _userManager_UserCreated;
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

            try
            {
                await _notificationManager.SendNotification(notification, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending notification", ex);
            }
        }

        async void _taskManager_TaskCompleted(object sender, GenericEventArgs<TaskResult> e)
        {
            var result = e.Argument;

            if (result.Status == TaskCompletionStatus.Failed)
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

                try
                {
                    await _notificationManager.SendNotification(notification, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error sending notification", ex);
                }
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

            try
            {
                await _notificationManager.SendNotification(notification, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending notification", ex);
            }
        }

        async void _installationManager_PackageInstallationCompleted(object sender, InstallationEventArgs e)
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
                Name = installationInfo.Name + " " + installationInfo.Version + " was installed",
                Description = e.PackageVersionInfo.description
            };

            try
            {
                await _notificationManager.SendNotification(notification, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error sending notification", ex);
            }
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
        }
    }
}
