using MediaBrowser.Model.Querying;

namespace MediaBrowser.Model.News
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
        /// <returns>QueryResult{NewsItem}.</returns>
        QueryResult<NewsItem> GetProductNews(NewsQuery query);
    }
}
