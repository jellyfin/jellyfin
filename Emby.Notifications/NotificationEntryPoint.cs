using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Activity;
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
        private readonly ILogger<NotificationEntryPoint> _logger;
        private readonly IActivityManager _activityManager;
        private readonly INotificationManager _notificationManager;
        private readonly ILibraryManager _libraryManager;
        private readonly IConfigurationManager _config;

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
        /// <param name="config">The configuration manager.</param>
        public NotificationEntryPoint(
            ILogger<NotificationEntryPoint> logger,
            IActivityManager activityManager,
            ILocalizationManager localization,
            INotificationManager notificationManager,
            ILibraryManager libraryManager,
            IConfigurationManager config)
        {
            _logger = logger;
            _activityManager = activityManager;
            _notificationManager = notificationManager;
            _libraryManager = libraryManager;
            _config = config;

            _coreNotificationTypes = new CoreNotificationTypes(localization).GetNotificationTypes().Select(i => i.Type).ToArray();
        }

        /// <inheritdoc />
        public Task RunAsync()
        {
            _activityManager.EntryCreated += OnActivityManagerEntryCreated;

            return Task.CompletedTask;
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
                _activityManager.EntryCreated -= OnActivityManagerEntryCreated;
                _disposed = true;
            }
        }
    }
}
