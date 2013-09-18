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
        private readonly INotificationsRepository _notificationsRepo;
        private readonly IInstallationManager _installationManager;
        private readonly IUserManager _userManager;
        private readonly ILogger _logger;

        private readonly ITaskManager _taskManager;

        public Notifications(IInstallationManager installationManager, INotificationsRepository notificationsRepo, IUserManager userManager, ILogger logger, ITaskManager taskManager)
        {
            _installationManager = installationManager;
            _notificationsRepo = notificationsRepo;
            _userManager = userManager;
            _logger = logger;
            _taskManager = taskManager;
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
            var notification = new Notification
            {
                UserId = e.Argument.Id,
                Category = "UserCreated",
                Name = "Welcome to Media Browser!",
                Description = "Check back here for more notifications."
            };

            try
            {
                await _notificationsRepo.AddNotification(notification, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error adding notification", ex);
            }
        }

        async void _taskManager_TaskCompleted(object sender, GenericEventArgs<TaskResult> e)
        {
            var result = e.Argument;

            if (result.Status == TaskCompletionStatus.Failed)
            {
                foreach (var user in _userManager
                    .Users
                    .Where(i => i.Configuration.IsAdministrator)
                    .ToList())
                {
                    var notification = new Notification
                    {
                        UserId = user.Id,
                        Category = "ScheduledTaskFailed",
                        Name = result.Name + " failed",
                        RelatedId = result.Name,
                        Description = result.ErrorMessage,
                        Level = NotificationLevel.Error
                    };

                    try
                    {
                        await _notificationsRepo.AddNotification(notification, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.ErrorException("Error adding notification", ex);
                    }
                }
            }
        }

        async void _installationManager_PluginUninstalled(object sender, GenericEventArgs<IPlugin> e)
        {
            var plugin = e.Argument;

            foreach (var user in _userManager
                .Users
                .Where(i => i.Configuration.IsAdministrator)
                .ToList())
            {
                var notification = new Notification
                {
                    UserId = user.Id,
                    Category = "PluginUninstalled",
                    Name = plugin.Name + " has been uninstalled",
                    RelatedId = plugin.Id.ToString()
                };

                try
                {
                    await _notificationsRepo.AddNotification(notification, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error adding notification", ex);
                }
            }
        }

        async void _installationManager_PackageInstallationCompleted(object sender, InstallationEventArgs e)
        {
            var installationInfo = e.InstallationInfo;

            foreach (var user in _userManager
                .Users
                .Where(i => i.Configuration.IsAdministrator)
                .ToList())
            {
                var notification = new Notification
                {
                    UserId = user.Id,
                    Category = "PackageInstallationCompleted",
                    Name = installationInfo.Name + " " + installationInfo.Version + " was installed",
                    RelatedId = installationInfo.Name,
                    Description = e.PackageVersionInfo.description
                };

                try
                {
                    await _notificationsRepo.AddNotification(notification, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error adding notification", ex);
                }
            }
        }

        async void _installationManager_PackageInstallationFailed(object sender, InstallationFailedEventArgs e)
        {
            var installationInfo = e.InstallationInfo;

            foreach (var user in _userManager
                .Users
                .Where(i => i.Configuration.IsAdministrator)
                .ToList())
            {
                var notification = new Notification
                {
                    UserId = user.Id,
                    Category = "PackageInstallationFailed",
                    Level = NotificationLevel.Error,
                    Name = installationInfo.Name + " " + installationInfo.Version + " installation failed",
                    RelatedId = installationInfo.Name,
                    Description = e.Exception.Message
                };

                try
                {
                    await _notificationsRepo.AddNotification(notification, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error adding notification", ex);
                }
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
