using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.News;
using MediaBrowser.Model.News;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace MediaBrowser.Server.Implementations.News
{
    public class NewsService : INewsService
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClient _httpClient;

        public NewsService(IApplicationPaths appPaths, IFileSystem fileSystem, IHttpClient httpClient)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
        }

        public async Task<QueryResult<NewsItem>> GetProductNews(NewsQuery query)
        {
            var path = Path.Combine(_appPaths.CachePath, "news.xml");

            await EnsureNewsFile(path).ConfigureAwait(false);

            var items = GetNewsItems(path);

            var itemsArray = items.ToArray();
            var count = itemsArray.Length;

            if (query.StartIndex.HasValue)
            {
                itemsArray = itemsArray.Skip(query.StartIndex.Value).ToArray();
            }

            if (query.Limit.HasValue)
            {
                itemsArray = itemsArray.Take(query.Limit.Value).ToArray();
            }

            return new QueryResult<NewsItem>
            {
                Items = itemsArray,
                TotalRecordCount = count
            };
        }

        private IEnumerable<NewsItem> GetNewsItems(string path)
        {
            var xmlDoc = new XmlDocument();

            xmlDoc.Load(path);

            return ParseRssItems(xmlDoc);
        }

        private IEnumerable<NewsItem> ParseRssItems(XmlDocument xmlDoc)
        {
            var nodes = xmlDoc.SelectNodes("rss/channel/item");

            if (nodes == null)
            {
                yield return null;
            }

            foreach (XmlNode node in nodes)
            {
                var newsItem = new NewsItem();

                newsItem.Title = ParseDocElements(node, "title");

                newsItem.Description = ParseDocElements(node, "description");

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

        private string ParseDocElements(XmlNode parent, string xPath)
        {
            var node = parent.SelectSingleNode(xPath);

            return node != null ? node.InnerText : "Unresolvable";
        }


        /// <summary>
        /// Ensures the news file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Task.</returns>
        private async Task EnsureNewsFile(string path)
        {
            var info = _fileSystem.GetFileSystemInfo(path);

            if (!info.Exists || (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(info)).TotalHours > 24)
            {
                var requestOptions = new HttpRequestOptions
                {
                    Url = "http://mediabrowser3.com/community/index.php?/blog/rss/1-media-browser-developers-blog",
                    Progress = new Progress<double>()
                };

                using (var stream = await _httpClient.Get(requestOptions).ConfigureAwait(false))
                {
                    using (var fileStream = _fileSystem.GetFileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                    {
                        await stream.CopyToAsync(fileStream).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
