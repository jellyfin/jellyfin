using MediaBrowser.Model.News;
using MediaBrowser.Model.Querying;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.News
{
    /// <summary>
    /// Interface INewsFeed
    /// </summary>
    public interface INewsService
    {
        /// <summary>
        /// Gets the product news.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable{NewsItem}.</returns>
        Task<QueryResult<NewsItem>> GetProductNews(NewsQuery query);
    }
}
