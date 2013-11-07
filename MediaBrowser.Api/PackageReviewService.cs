using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Constants;
using MediaBrowser.Common.Net;
using ServiceStack.ServiceHost;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class InstallPackage
    /// </summary>
    [Route("/PackageReviews/{Id}", "POST")]
    [Api(("Creates or updates a package review"))]
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


    public class PackageReviewService : BaseApiService
    {
        private readonly IHttpClient _httpClient;
        private readonly INetworkManager _netManager;

        public PackageReviewService(IHttpClient client, INetworkManager net)
        {
            _httpClient = client;
            _netManager = net;
        }

        public void Post(CreateReviewRequest request)
        {
            var review = new Dictionary<string, string>
                             { { "id", request.Id.ToString(CultureInfo.InvariantCulture) },
                               { "mac", _netManager.GetMacAddress() },
                               { "rating", request.Rating.ToString(CultureInfo.InvariantCulture) },
                               { "recommend", request.Recommend.ToString() },
                               { "title", request.Title },
                               { "review", request.Review },
                             };

            Task.WaitAll(_httpClient.Post(Constants.MbAdminUrl + "/service/packageReview/update", review, CancellationToken.None));
        }
    }
}
