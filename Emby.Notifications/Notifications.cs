using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Tasks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Threading;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Events;

namespace Emby.Notifications
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

        private readonly ILibraryManager _libraryManager;
        private readonly ISessionManager _sessionManager;
        private readonly IServerApplicationHost _appHost;
        private readonly ITimerFactory _timerFactory;

        private ITimer LibraryUpdateTimer { get; set; }
        private readonly object _libraryChangedSyncLock = new object();

        private readonly IConfigurationManager _config;
        private readonly IDeviceManager _deviceManager;
        private readonly ILocalizationManager _localization;
        private readonly IActivityManager _activityManager;

        private string[] _coreNotificationTypes;

        public Notifications(IInstallationManager installationManager, IActivityManager activityManager, ILocalizationManager localization, IUserManager userManager, ILogger logger, ITaskManager taskManager, INotificationManager notificationManager, ILibraryManager libraryManager, ISessionManager sessionManager, IServerApplicationHost appHost, IConfigurationManager config, IDeviceManager deviceManager, ITimerFactory timerFactory)
        {
            _installationManager = installationManager;
            _userManager = userManager;
            _logger = logger;
            _taskManager = taskManager;
            _notificationManager = notificationManager;
            _libraryManager = libraryManager;
            _sessionManager = sessionManager;
            _appHost = appHost;
            _config = config;
            _deviceManager = deviceManager;
            _timerFactory = timerFactory;
            _localization = localization;
            _activityManager = activityManager;

            _coreNotificationTypes = new CoreNotificationTypes(localization, appHost).GetNotificationTypes().Select(i => i.Type).ToArray();
        }

        public void Run()
        {
            _libraryManager.ItemAdded += _libraryManager_ItemAdded;
            _appHost.HasPendingRestartChanged += _appHost_HasPendingRestartChanged;
            _appHost.HasUpdateAvailableChanged += _appHost_HasUpdateAvailableChanged;
            _activityManager.EntryCreated += _activityManager_EntryCreated;
        }

        private async void _appHost_HasPendingRestartChanged(object sender, EventArgs e)
        {
            var type = NotificationType.ServerRestartRequired.ToString();

            var notification = new NotificationRequest
            {
                NotificationType = type,
                Name = string.Format(_localization.GetLocalizedString("ServerNameNeedsToBeRestarted"), _appHost.Name)
            };

            await SendNotification(notification, null).ConfigureAwait(false);
        }

        private async void _activityManager_EntryCreated(object sender, GenericEventArgs<ActivityLogEntry> e)
        {
            var entry = e.Argument;

            var type = entry.Type;

            if (string.IsNullOrEmpty(type) || !_coreNotificationTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            {
                return;
            }

            var userId = e.Argument.UserId;

            if (!userId.Equals(Guid.Empty) && !GetOptions().IsEnabledToMonitorUser(type, userId))
            {
                return;
            }

            var notification = new NotificationRequest
            {
                NotificationType = type,
                Name = entry.Name,
                Description = entry.Overview
            };

            await SendNotification(notification, null).ConfigureAwait(false);
        }

        private NotificationOptions GetOptions()
        {
            return _config.GetConfiguration<NotificationOptions>("notifications");
        }

        async void _appHost_HasUpdateAvailableChanged(object sender, EventArgs e)
        {
            // This notification is for users who can't auto-update (aka running as service)
            if (!_appHost.HasUpdateAvailable || _appHost.CanSelfUpdate)
            {
                return;
            }

            var type = NotificationType.ApplicationUpdateAvailable.ToString();

            var notification = new NotificationRequest
            {
                Description = "Please see jellyfin.media for details.",
                NotificationType = type,
                Name = _localization.GetLocalizedString("NewVersionIsAvailable")
            };

            await SendNotification(notification, null).ConfigureAwait(false);
        }

        private readonly List<BaseItem> _itemsAdded = new List<BaseItem>();
        void _libraryManager_ItemAdded(object sender, ItemChangeEventArgs e)
        {
            if (!FilterItem(e.Item))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
                if (LibraryUpdateTimer == null)
                {
                    LibraryUpdateTimer = _timerFactory.Create(LibraryUpdateTimerCallback, null, 5000,
                                                   Timeout.Infinite);
                }
                else
                {
                    LibraryUpdateTimer.Change(5000, Timeout.Infinite);
                }

                _itemsAdded.Add(e.Item);
            }
        }

        private bool FilterItem(BaseItem item)
        {
            if (item.IsFolder)
            {
                return false;
            }

            if (!item.HasPathProtocol)
            {
                return false;
            }

            if (item is IItemByName)
            {
                return false;
            }

            return item.SourceType == SourceType.Library;
        }

        private async void LibraryUpdateTimerCallback(object state)
        {
            List<BaseItem> items;

            lock (_libraryChangedSyncLock)
            {
                items = _itemsAdded.ToList();
                _itemsAdded.Clear();
                DisposeLibraryUpdateTimer();
            }

            items = items.Take(10).ToList();

            foreach (var item in items)
            {
                var notification = new NotificationRequest
                {
                    NotificationType = NotificationType.NewLibraryContent.ToString(),
                    Name = string.Format(_localization.GetLocalizedString("ValueHasBeenAddedToLibrary"), GetItemName(item)),
                    Description = item.Overview
                };

                await SendNotification(notification, item).ConfigureAwait(false);
            }
        }

        public static string GetItemName(BaseItem item)
        {
            var name = item.Name;
            var episode = item as Episode;
            if (episode != null)
            {
                if (episode.IndexNumber.HasValue)
                {
                    name = string.Format("Ep{0} - {1}", episode.IndexNumber.Value.ToString(CultureInfo.InvariantCulture), name);
                }
                if (episode.ParentIndexNumber.HasValue)
                {
                    name = string.Format("S{0}, {1}", episode.ParentIndexNumber.Value.ToString(CultureInfo.InvariantCulture), name);
                }
            }

            var hasSeries = item as IHasSeries;

            if (hasSeries != null)
            {
                name = hasSeries.SeriesName + " - " + name;
            }

            var hasAlbumArtist = item as IHasAlbumArtist;
            if (hasAlbumArtist != null)
            {
                var artists = hasAlbumArtist.AlbumArtists;

                if (artists.Length > 0)
                {
                    name = artists[0] + " - " + name;
                }
            }
            else
            {
                var hasArtist = item as IHasArtist;
                if (hasArtist != null)
                {
                    var artists = hasArtist.Artists;

                    if (artists.Length > 0)
                    {
                        name = artists[0] + " - " + name;
                    }
                }
            }

            return name;
        }

        private async Task SendNotification(NotificationRequest notification, BaseItem relatedItem)
        {
            try
            {
                await _notificationManager.SendNotification(notification, relatedItem, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
            }
        }

        public void Dispose()
        {
            DisposeLibraryUpdateTimer();

            _libraryManager.ItemAdded -= _libraryManager_ItemAdded;
            _appHost.HasPendingRestartChanged -= _appHost_HasPendingRestartChanged;
            _appHost.HasUpdateAvailableChanged -= _appHost_HasUpdateAvailableChanged;
            _activityManager.EntryCreated -= _activityManager_EntryCreated;
        }

        private void DisposeLibraryUpdateTimer()
        {
            if (LibraryUpdateTimer != null)
            {
                LibraryUpdateTimer.Dispose();
                LibraryUpdateTimer = null;
            }
        }
    }
}
