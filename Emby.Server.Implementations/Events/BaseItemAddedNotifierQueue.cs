using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;

namespace Emby.Server.Implementations.Events
{
    /// <summary>
    /// Base item added notification queue.
    /// </summary>
    public class BaseItemAddedNotifierQueue : IDisposable
    {
        private readonly List<BaseItem> _items;
        private readonly object _lock = new object();
        private readonly INotificationManager _notificationManager;
        private readonly ILocalizationManager _localizationManager;

        private Timer _timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemAddedNotifierQueue"/> class.
        /// </summary>
        /// <param name="notificationManager">Instance of the <see cref="INotificationManager"/> interface.</param>
        /// <param name="localizationManager">Instance of the <see cref="ILocalizationManager"/> interface.</param>
        public BaseItemAddedNotifierQueue(INotificationManager notificationManager, ILocalizationManager localizationManager)
        {
            _notificationManager = notificationManager;
            _localizationManager = localizationManager;

            _items = new List<BaseItem>();
        }

        /// <summary>
        /// Add item to notification queue.
        /// </summary>
        /// <param name="item">Item to add.</param>
        public void EnqueueItem(BaseItem item)
        {
            lock (_lock)
            {
                _items.Add(item);
                ResetTimer();
            }
        }

        private static string GetItemName(BaseItem item)
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

        private void ResetTimer()
        {
            if (_timer == null)
            {
                _timer = new Timer(
                    TimerCallback,
                    null,
                    5000,
                    Timeout.Infinite);
            }
            else
            {
                _timer.Change(5000, Timeout.Infinite);
            }
        }

        private async void TimerCallback(object state)
        {
            List<BaseItem> items;

            lock (_lock)
            {
                items = _items.ToList();
                _items.Clear();
                _timer!.Dispose(); // Shouldn't be null as it just set off this callback
                _timer = null;
            }

            for (var i = 0; i < items.Count && i < 10; i++)
            {
                var notification = new NotificationRequest
                {
                    NotificationType = NotificationType.NewLibraryContent.ToString(),
                    Name = string.Format(
                        CultureInfo.InvariantCulture,
                        _localizationManager.GetLocalizedString("ValueHasBeenAddedToLibrary"),
                        GetItemName(items[i])),
                    Description = items[i].Overview
                };

                await _notificationManager.SendNotification(notification, items[i], CancellationToken.None).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose all objects.
        /// </summary>
        /// <param name="disposing">Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_lock)
                {
                    _timer?.Dispose();
                }
            }
        }
    }
}
