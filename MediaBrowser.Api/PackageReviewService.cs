using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using ServiceStack;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class InstallPackage
    /// </summary>
    [Route("/Packages/Reviews/{Id}", "POST", Summary = "Creates or updates a package review")]
    public class CreateReviewRequest : IReturnVoid
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        /// <value>The Id.</value>
        [ApiMember(Name = "Id", Description = "Package Id", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "POST")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the rating.
        /// </summary>
        /// <value>The review.</value>
        [ApiMember(Name = "Rating", Description = "The rating value (1-5)", IsRequired = true, DataType = "int", ParameterType = "query", Verb = "POST")]
        public int Rating { get; set; }

        /// <summary>
        /// Gets or sets the recommend value.
        /// </summary>
        /// <value>Whether or not this review recommends this item.</value>
        [ApiMember(Name = "Recommend", Description = "Whether or not this review recommends this item", IsRequired = true, DataType = "bool", ParameterType = "query", Verb = "POST")]
        public bool Recommend { get; set; }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [ApiMember(Name = "Title", Description = "Optional short description of review.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the full review.
        /// </summary>
        /// <value>The full review.</value>
        [ApiMember(Name = "Review", Description = "Optional full review.", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Review { get; set; }
    }

    /// <summary>
    /// Class InstallPackage
    /// </summary>
    [Route("/Packages/{Id}/Reviews", "GET", Summary = "Gets reviews for a package")]
    public class ReviewRequest : IReturn<List<PackageReviewInfo>>
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        /// <value>The Id.</value>
        [ApiMember(Name = "Id", Description = "Package Id", IsRequired = true, DataType = "int", ParameterType = "path", Verb = "GET")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the max rating.
        /// </summary>
        /// <value>The max rating.</value>
        [ApiMember(Name = "MaxRating", Description = "Retrieve only reviews less than or equal to this", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int MaxRating { get; set; }

        /// <summary>
        /// Gets or sets the min rating.
        /// </summary>
        /// <value>The max rating.</value>
        [ApiMember(Name = "MinRating", Description = "Retrieve only reviews greator than or equal to this", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int MinRating { get; set; }

        /// <summary>
        /// Only retrieve reviews with at least a short review.
        /// </summary>
        /// <value>True if should only get reviews with a title.</value>
        [ApiMember(Name = "ForceTitle", Description = "Whether or not to restrict results to those with a title", IsRequired = false, DataType = "bool", ParameterType = "query", Verb = "GET")]
        public bool ForceTitle { get; set; }

        /// <summary>
        /// Gets or sets the limit for the query.
        /// </summary>
        /// <value>The max rating.</value>
        [ApiMember(Name = "Limit", Description = "Limit the result to this many reviews (ordered by latest)", IsRequired = false, DataType = "int", ParameterType = "query", Verb = "GET")]
        public int Limit { get; set; }

    }

    [Authenticated]
    public class PackageReviewService : BaseApiService
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _serializer;
        private const string MbAdminUrl = "https://www.mb3admin.com/admin/";
        private readonly IServerApplicationHost _appHost;

        public PackageReviewService(IHttpClient httpClient, IJsonSerializer serializer, IServerApplicationHost appHost)
        {
            _httpClient = httpClient;
            _serializer = serializer;
            _appHost = appHost;
        }

        public async Task<object> Get(ReviewRequest request)
        {
            var parms = "?id=" + request.Id;

            if (request.MaxRating > 0)
            {
                parms += "&max=" + request.MaxRating;
            }
            if (request.MinRating > 0)
            {
                parms += "&min=" + request.MinRating;
            }
            if (request.MinRating > 0)
            {
                parms += "&limit=" + request.Limit;
            }
            if (request.ForceTitle)
            {
                parms += "&title=true";
            }

            using (var result = await _httpClient.Get(MbAdminUrl + "/service/packageReview/retrieve" + parms, CancellationToken.None)
                            .ConfigureAwait(false))
            {
                var reviews = _serializer.DeserializeFromStream<List<PackageReviewInfo>>(result);

                return ToOptimizedResult(reviews);
            }
        }

        public void Post(CreateReviewRequest request)
        {
            var reviewText = WebUtility.HtmlEncode(request.Review ?? string.Empty);
            var title = WebUtility.HtmlEncode(request.Title ?? string.Empty);

            var review = new Dictionary<string, string>
                             { { "id", request.Id.ToString(CultureInfo.InvariantCulture) },
                               { "mac", _appHost.SystemId },
                               { "rating", request.Rating.ToString(CultureInfo.InvariantCulture) },
                               { "recommend", request.Recommend.ToString() },
                               { "title", title },
                               { "review", reviewText },
                             };

            Task.WaitAll(_httpClient.Post(MbAdminUrl + "/service/packageReview/update", review, CancellationToken.None));
        }
    }
}
