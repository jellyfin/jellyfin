using MediaBrowser.Model.News;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Api
{
    [Route("/News/Product", "GET", Summary = "Gets the latest product news.")]
    public class GetProductNews : IReturn<QueryResult<NewsItem>>
    {
        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        [ApiMember(Name = "StartIndex", Description = "Optional. The record index to start at. All items with a lower index will be dropped from the results.", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        [ApiMember(Name = "Limit", Description = "Optional. The maximum number of records to return", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int? Limit { get; set; }
    }

    public class NewsService : BaseApiService
    {
        private readonly INewsService _newsService;

        public NewsService(INewsService newsService)
        {
            _newsService = newsService;
        }

        public object Get(GetProductNews request)
        {
            var result = _newsService.GetProductNews(new NewsQuery
            {
                StartIndex = request.StartIndex,
                Limit = request.Limit

            });

            return ToOptimizedSerializedResultUsingCache(result);
        }
    }
}
