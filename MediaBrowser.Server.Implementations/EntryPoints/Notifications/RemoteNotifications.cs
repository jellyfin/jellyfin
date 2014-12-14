using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.EntryPoints.Notifications
{
    public class RemoteNotifications : IServerEntryPoint
    {
        private const string Url = "https://www.mb3admin.com/admin/service/MB3ServerNotifications.json";

        private Timer _timer;
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;
        private readonly IUserManager _userManager;
        private readonly IFileSystem _fileSystem;

        private readonly TimeSpan _frequency = TimeSpan.FromHours(6);
        private readonly TimeSpan _maxAge = TimeSpan.FromDays(31);

        private readonly INotificationManager _notificationManager;

        public RemoteNotifications(IApplicationPaths appPaths, ILogger logger, IHttpClient httpClient, IJsonSerializer json, IUserManager userManager, IFileSystem fileSystem, INotificationManager notificationManager)
        {
            _appPaths = appPaths;
            _logger = logger;
            _httpClient = httpClient;
            _json = json;
            _userManager = userManager;
            _fileSystem = fileSystem;
            _notificationManager = notificationManager;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public void Run()
        {
            _timer = new Timer(OnTimerFired, null, TimeSpan.FromMilliseconds(500), _frequency);
        }

        /// <summary>
        /// Called when [timer fired].
        /// </summary>
        /// <param name="state">The state.</param>
        private async void OnTimerFired(object state)
        {
            var dataPath = Path.Combine(_appPaths.DataPath, "remotenotifications.json");

            var lastRunTime = File.Exists(dataPath) ? _fileSystem.GetLastWriteTimeUtc(dataPath) : DateTime.MinValue;

            try
            {
                await DownloadNotifications(dataPath, lastRunTime).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error downloading remote notifications", ex);
            }
        }

        /// <summary>
        /// Downloads the notifications.
        /// </summary>
        /// <param name="dataPath">The data path.</param>
        /// <param name="lastRunTime">The last run time.</param>
        /// <returns>Task.</returns>
        private async Task DownloadNotifications(string dataPath, DateTime lastRunTime)
        {
            using (var stream = await _httpClient.Get(new HttpRequestOptions
            {
                Url = Url

            }).ConfigureAwait(false))
            {
                var notifications = _json.DeserializeFromStream<RemoteNotification[]>(stream);

                File.WriteAllText(dataPath, string.Empty);

                await CreateNotifications(notifications, lastRunTime).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates the notifications.
        /// </summary>
        /// <param name="notifications">The notifications.</param>
        /// <param name="lastRunTime">The last run time.</param>
        /// <returns>Task.</returns>
        private async Task CreateNotifications(IEnumerable<RemoteNotification> notifications, DateTime lastRunTime)
        {
            // Only show notifications that are active, new since last download, and not older than max age
            var notificationList = notifications
                .Where(i => string.Equals(i.active, "1") && i.date.ToUniversalTime() > lastRunTime && (DateTime.UtcNow - i.date.ToUniversalTime()) <= _maxAge)
                .ToList();

            var userIds = _userManager.Users.Select(i => i.Id.ToString("N")).ToList();

            foreach (var notification in notificationList)
            {
                await _notificationManager.SendNotification(new NotificationRequest
                {
                    Date = notification.date,
                    Name = notification.name,
                    Description = notification.description,
                    Url = notification.url,
                    UserIds = userIds

                }, CancellationToken.None).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private class RemoteNotification
        {
            public string id { get; set; }
            public DateTime date { get; set; }
            public string name { get; set; }
            public string description { get; set; }
            public string category { get; set; }
            public string url { get; set; }
            public object imageUrl { get; set; }
            public string active { get; set; }
        }
    }
}
