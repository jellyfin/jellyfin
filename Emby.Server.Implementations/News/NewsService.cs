using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.News;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Emby.Server.Implementations.News
{
    public class NewsService : INewsService
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IJsonSerializer _json;

        public NewsService(IApplicationPaths appPaths, IJsonSerializer json)
        {
            _appPaths = appPaths;
            _json = json;
        }

        public QueryResult<NewsItem> GetProductNews(NewsQuery query)
        {
            try
            {
                return GetProductNewsInternal(query);
            }
            catch (FileNotFoundException)
            {
                // No biggie
                return new QueryResult<NewsItem>
                {
                    Items = new NewsItem[] { }
                };
            }
            catch (IOException)
            {
                // No biggie
                return new QueryResult<NewsItem>
                {
                    Items = new NewsItem[] { }
                };
            }
        }

        private QueryResult<NewsItem> GetProductNewsInternal(NewsQuery query)
        {
            var path = Path.Combine(_appPaths.CachePath, "news.json");

            var items = GetNewsItems(path).OrderByDescending(i => i.Date);

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
            return _json.DeserializeFromFile<List<NewsItem>>(path);
        }
    }
}
