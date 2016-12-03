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
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Threading;

namespace Emby.Server.Implementations.News
{
    public class NewsEntryPoint : IServerEntryPoint
    {
        private ITimer _timer;
        private readonly IHttpClient _httpClient;
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;

        private readonly INotificationManager _notifications;
        private readonly IUserManager _userManager;

        private readonly TimeSpan _frequency = TimeSpan.FromHours(24);
        private readonly ITimerFactory _timerFactory;

        public NewsEntryPoint(IHttpClient httpClient, IApplicationPaths appPaths, IFileSystem fileSystem, ILogger logger, IJsonSerializer json, INotificationManager notifications, IUserManager userManager, ITimerFactory timerFactory)
        {
            _httpClient = httpClient;
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _logger = logger;
            _json = json;
            _notifications = notifications;
            _userManager = userManager;
            _timerFactory = timerFactory;
        }

        public void Run()
        {
            _timer = _timerFactory.Create(OnTimerFired, null, TimeSpan.FromMilliseconds(500), _frequency);
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
                using (var reader = XmlReader.Create(stream))
                {
                    var news = ParseRssItems(reader).ToList();

                    _json.SerializeToFile(news, path);

                    await CreateNotifications(news, lastUpdate, CancellationToken.None).ConfigureAwait(false);
                }
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

        private IEnumerable<NewsItem> ParseRssItems(XmlReader reader)
        {
            reader.MoveToContent();
            reader.Read();

            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "channel":
                            {
                                if (!reader.IsEmptyElement)
                                {
                                    using (var subReader = reader.ReadSubtree())
                                    {
                                        return ParseFromChannelNode(subReader);
                                    }
                                }
                                else
                                {
                                    reader.Read();
                                }
                                break;
                            }
                        default:
                            {
                                reader.Skip();
                                break;
                            }
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            return new List<NewsItem>();
        }

        private IEnumerable<NewsItem> ParseFromChannelNode(XmlReader reader)
        {
            var list = new List<NewsItem>();

            reader.MoveToContent();
            reader.Read();

            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "item":
                            {
                                if (!reader.IsEmptyElement)
                                {
                                    using (var subReader = reader.ReadSubtree())
                                    {
                                        list.Add(ParseItem(subReader));
                                    }
                                }
                                else
                                {
                                    reader.Read();
                                }
                                break;
                            }
                        default:
                            {
                                reader.Skip();
                                break;
                            }
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            return list;
        }

        private NewsItem ParseItem(XmlReader reader)
        {
            var item = new NewsItem();

            reader.MoveToContent();
            reader.Read();

            while (!reader.EOF && reader.ReadState == ReadState.Interactive)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    switch (reader.Name)
                    {
                        case "title":
                        {
                            item.Title = reader.ReadElementContentAsString();
                            break;
                        }
                        case "link":
                            {
                                item.Link = reader.ReadElementContentAsString();
                                break;
                            }
                        case "description":
                            {
                                item.DescriptionHtml = reader.ReadElementContentAsString();
                                item.Description = item.DescriptionHtml.StripHtml();
                                break;
                            }
                        case "pubDate":
                            {
                                var date = reader.ReadElementContentAsString();
                                DateTime parsedDate;

                                if (DateTime.TryParse(date, out parsedDate))
                                {
                                    item.Date = parsedDate;
                                }
                                break;
                            }
                        default:
                            {
                                reader.Skip();
                                break;
                            }
                    }
                }
                else
                {
                    reader.Read();
                }
            }

            return item;
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
