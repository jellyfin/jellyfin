using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.News;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CommonIO;
using MediaBrowser.Common.Threading;

namespace MediaBrowser.Server.Implementations.News
{
    public class NewsEntryPoint : IServerEntryPoint
    {
        private PeriodicTimer _timer;
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;

        private readonly INotificationManager _notifications;
        private readonly IUserManager _userManager;

        private readonly TimeSpan _frequency = TimeSpan.FromHours(24);

        public NewsEntryPoint(IHttpClient httpClient, IApplicationPaths appPaths, IFileSystem fileSystem, ILogger logger, IJsonSerializer json, INotificationManager notifications, IUserManager userManager)
        {
            _httpClient = httpClient;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _logger = logger;
            _json = json;
            _notifications = notifications;
            _userManager = userManager;
        }

        public void Run()
        {
            _timer = new PeriodicTimer(OnTimerFired, null, TimeSpan.FromMilliseconds(500), _frequency);
        }

        /// <summary>
        /// Called when [timer fired].
        /// </summary>
        /// <param name="state">The state.</param>
        private async void OnTimerFired(object state)
        {
            var path = Path.Combine(_appPaths.CachePath, "news.json");

            try
            {
                await DownloadNews(path).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error downloading news", ex);
            }
        }

        private async Task DownloadNews(string path)
        {
            DateTime? lastUpdate = null;

			if (_fileSystem.FileExists(path))
            {
                lastUpdate = _fileSystem.GetLastWriteTimeUtc(path);
            }

            var requestOptions = new HttpRequestOptions
            {
                Url = "http://emby.media/community/index.php?/blog/rss/1-media-browser-developers-blog",
                Progress = new Progress<double>(),
                UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2490.42 Safari/537.36",
                BufferContent = false
            };

            using (var stream = await _httpClient.Get(requestOptions).ConfigureAwait(false))
            {
                var doc = new XmlDocument();
                doc.Load(stream);

                var news = ParseRssItems(doc).ToList();

                _json.SerializeToFile(news, path);

                await CreateNotifications(news, lastUpdate, CancellationToken.None).ConfigureAwait(false);
            }
        }

        private Task CreateNotifications(List<NewsItem> items, DateTime? lastUpdate, CancellationToken cancellationToken)
        {
            if (lastUpdate.HasValue)
            {
                items = items.Where(i => i.Date.ToUniversalTime() >= lastUpdate.Value)
                    .ToList();
            }

            var tasks = items.Select(i => _notifications.SendNotification(new NotificationRequest
            {
                Date = i.Date,
                Name = i.Title,
                Description = i.Description,
                Url = i.Link,
                UserIds = _userManager.Users.Select(u => u.Id.ToString("N")).ToList()

            }, cancellationToken));

            return Task.WhenAll(tasks);
        }

        private IEnumerable<NewsItem> ParseRssItems(XmlDocument xmlDoc)
        {
            var nodes = xmlDoc.SelectNodes("rss/channel/item");

            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    var newsItem = new NewsItem();

                    newsItem.Title = ParseDocElements(node, "title");

                    newsItem.DescriptionHtml = ParseDocElements(node, "description");
                    newsItem.Description = newsItem.DescriptionHtml.StripHtml();

                    newsItem.Link = ParseDocElements(node, "link");

                    var date = ParseDocElements(node, "pubDate");
                    DateTime parsedDate;

                    if (DateTime.TryParse(date, out parsedDate))
                    {
                        newsItem.Date = parsedDate;
                    }

                    yield return newsItem;
                }
            }
        }

        private string ParseDocElements(XmlNode parent, string xPath)
        {
            var node = parent.SelectSingleNode(xPath);

            return node != null ? node.InnerText : string.Empty;
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
