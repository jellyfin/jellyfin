using System.Threading;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Querying;
using ServiceStack.ServiceHost;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class GetCriticReviews
    /// </summary>
    [Route("/Items/{Id}/CriticReviews", "GET")]
    [Api(Description = "Gets critic reviews for an item")]
    public class GetCriticReviews : IReturn<ItemReviewsResult>
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ApiMember(Name = "Id", Description = "Item Id", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "GET")]
        public string Id { get; set; }

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

    [Route("/Library/Refresh", "POST")]
    [Api(Description = "Starts a library scan")]
    public class RefreshLibrary : IReturnVoid
    {
    }
    
    /// <summary>
    /// Class LibraryService
    /// </summary>
    public class LibraryService : BaseApiService
    {
        /// <summary>
        /// The _item repo
        /// </summary>
        private readonly IItemRepository _itemRepo;

        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LibraryService" /> class.
        /// </summary>
        /// <param name="itemRepo">The item repo.</param>
        /// <param name="libraryManager">The library manager.</param>
        public LibraryService(IItemRepository itemRepo, ILibraryManager libraryManager)
        {
            _itemRepo = itemRepo;
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.Object.</returns>
        public object Get(GetCriticReviews request)
        {
            var result = GetCriticReviewsAsync(request).Result;

            return ToOptimizedResult(result);
        }

        /// <summary>
        /// Posts the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        public void Post(RefreshLibrary request)
        {
            _libraryManager.ValidateMediaLibrary(new Progress<double>(), CancellationToken.None);
        }

        /// <summary>
        /// Gets the critic reviews async.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Task{ItemReviewsResult}.</returns>
        private async Task<ItemReviewsResult> GetCriticReviewsAsync(GetCriticReviews request)
        {
            var reviews = await _itemRepo.GetCriticReviews(new Guid(request.Id)).ConfigureAwait(false);

            var reviewsArray = reviews.ToArray();

            var result = new ItemReviewsResult
            {
                TotalRecordCount = reviewsArray.Length
            };

            if (request.StartIndex.HasValue)
            {
                reviewsArray = reviewsArray.Skip(request.StartIndex.Value).ToArray();
            }
            if (request.Limit.HasValue)
            {
                reviewsArray = reviewsArray.Take(request.Limit.Value).ToArray();
            }

            result.ItemReviews = reviewsArray;

            return result;
        }
    }
}
