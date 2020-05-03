using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;
using Microsoft.Extensions.Logging;

namespace Emby.Notifications
{
    /// <summary>
    /// Creates notifications for various system events.
    /// </summary>
    public class NotificationEntryPoint : IServerEntryPoint
    {
        private readonly ILogger _logger;
        private readonly IActivityManager _activityManager;
        private readonly ILocalizationManager _localization;
        private readonly INotificationManager _notificationManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerApplicationHost _appHost;
        private readonly IConfigurationManager _config;

        private readonly object _libraryChangedSyncLock = new object();
        private readonly List<BaseItem> _itemsAdded = new List<BaseItem>();

        private Timer? _libraryUpdateTimer;

        private string[] _coreNotificationTypes;

        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationEntryPoint" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="activityManager">The activity manager.</param>
        /// <param name="localization">The localization manager.</param>
        /// <param name="notificationManager">The notification manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="appHost">The application host.</param>
        /// <param name="config">The configuration manager.</param>
        public NotificationEntryPoint(
            ILogger<NotificationEntryPoint> logger,
            IActivityManager activityManager,
            ILocalizationManager localization,
            INotificationManager notificationManager,
            ILibraryManager libraryManager,
            IServerApplicationHost appHost,
            IConfigurationManager config)
        {
            _logger = logger;
            _activityManager = activityManager;
            _localization = localization;
            _notificationManager = notificationManager;
            _libraryManager = libraryManager;
            _appHost = appHost;
            _config = config;

            _coreNotificationTypes = new CoreNotificationTypes(localization).GetNotificationTypes().Select(i => i.Type).ToArray();
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            _libraryManager.ItemAdded += OnLibraryManagerItemAdded;
            _appHost.HasPendingRestartChanged += OnAppHostHasPendingRestartChanged;
            _appHost.HasUpdateAvailableChanged += OnAppHostHasUpdateAvailableChanged;
            _activityManager.EntryCreated += OnActivityManagerEntryCreated;

            return Task.CompletedTask;
        }

        private async void OnAppHostHasPendingRestartChanged(object sender, EventArgs e)
        {
            var type = NotificationType.ServerRestartRequired.ToString();

            var notification = new NotificationRequest
            {
                NotificationType = type,
                Name = string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("ServerNameNeedsToBeRestarted"),
                    _appHost.Name)
            };

            await SendNotification(notification, null).ConfigureAwait(false);
        }

        private async void OnActivityManagerEntryCreated(object sender, GenericEventArgs<ActivityLogEntry> e)
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

        private async void OnAppHostHasUpdateAvailableChanged(object sender, EventArgs e)
        {
            if (!_appHost.HasUpdateAvailable)
            {
                return;
            }

            var type = NotificationType.ApplicationUpdateAvailable.ToString();

            var notification = new NotificationRequest
            {
                Description = "Please see jellyfin.org for details.",
                NotificationType = type,
                Name = _localization.GetLocalizedString("NewVersionIsAvailable")
            };

            await SendNotification(notification, null).ConfigureAwait(false);
        }

        private void OnLibraryManagerItemAdded(object sender, ItemChangeEventArgs e)
        {
            if (!FilterItem(e.Item))
            {
                return;
            }

            lock (_libraryChangedSyncLock)
            {
                if (_libraryUpdateTimer == null)
                {
                    _libraryUpdateTimer = new Timer(
                        LibraryUpdateTimerCallback,
                        null,
                        5000,
                        Timeout.Infinite);
                }
                else
                {
                    _libraryUpdateTimer.Change(5000, Timeout.Infinite);
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
                _libraryUpdateTimer!.Dispose(); // Shouldn't be null as it just set off this callback
                _libraryUpdateTimer = null;
            }

            items = items.Take(10).ToList();

            foreach (var item in items)
            {
                var notification = new NotificationRequest
                {
                    NotificationType = NotificationType.NewLibraryContent.ToString(),
                    Name = string.Format(
                        CultureInfo.InvariantCulture,
                        _localization.GetLocalizedString("ValueHasBeenAddedToLibrary"),
                        GetItemName(item)),
                    Description = item.Overview
                };

                await SendNotification(notification, item).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates a human readable name for the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>A human readable name for the item.</returns>
        public static string GetItemName(BaseItem item)
        {
            var name = item.Name;
            if (item is Episode episode)
            {
                if (episode.IndexNumber.HasValue)
                {
                    name = string.Format(
                        CultureInfo.InvariantCulture,
                        "Ep{0} - {1}",
                        episode.IndexNumber.Value,
                        name);
                }

                if (episode.ParentIndexNumber.HasValue)
                {
                    name = string.Format(
                        CultureInfo.InvariantCulture,
                        "S{0}, {1}",
                        episode.ParentIndexNumber.Value,
                        name);
                }
            }

            if (item is IHasSeries hasSeries)
            {
                name = hasSeries.SeriesName + " - " + name;
            }

            if (item is IHasAlbumArtist hasAlbumArtist)
            {
                var artists = hasAlbumArtist.AlbumArtists;

                if (artists.Count > 0)
                {
                    name = artists[0] + " - " + name;
                }
            }
            else if (item is IHasArtist hasArtist)
            {
                var artists = hasArtist.Artists;

                if (artists.Count > 0)
                {
                    name = artists[0] + " - " + name;
                }
            }

            return name;
        }

        private async Task SendNotification(NotificationRequest notification, BaseItem? relatedItem)
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

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _libraryUpdateTimer?.Dispose();
            }

            _libraryUpdateTimer = null;

            _libraryManager.ItemAdded -= OnLibraryManagerItemAdded;
            _appHost.HasPendingRestartChanged -= OnAppHostHasPendingRestartChanged;
            _appHost.HasUpdateAvailableChanged -= OnAppHostHasUpdateAvailableChanged;
            _activityManager.EntryCreated -= OnActivityManagerEntryCreated;

            _disposed = true;
        }
    }
}
