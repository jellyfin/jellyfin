using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Providers.Movies
{
    /// <summary>
    /// Class RottenTomatoesMovieProvider
    /// </summary>
    public class RottenTomatoesProvider : BaseMetadataProvider
    {
        // http://developer.rottentomatoes.com/iodocs

        private const string MoviesReviews = @"movies/{1}/reviews.json?review_type=top_critic&page_limit=10&page=1&country=us&apikey={0}";

        private const string ApiKey = "x9wjnvv39ntjmt9zs95nm7bg";

        private const string BasicUrl = @"http://api.rottentomatoes.com/api/public/v1.0/";
        private const string MovieImdb = @"movie_alias.json?id={1}&type=imdb&apikey={0}";

        private readonly SemaphoreSlim _rottenTomatoesResourcePool = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        protected IJsonSerializer JsonSerializer { get; private set; }

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RottenTomatoesMovieProvider"/> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="httpClient">The HTTP client.</param>
        public RottenTomatoesProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
            : base(logManager, configurationManager)
        {
            JsonSerializer = jsonSerializer;
            HttpClient = httpClient;
        }

        /// <summary>
        /// Gets the provider version.
        /// </summary>
        /// <value>The provider version.</value>
        protected override string ProviderVersion
        {
            get
            {
                return "5";
            }
        }

        /// <summary>
        /// Gets a value indicating whether [requires internet].
        /// </summary>
        /// <value><c>true</c> if [requires internet]; otherwise, <c>false</c>.</value>
        public override bool RequiresInternet
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [refresh on version change].
        /// </summary>
        /// <value><c>true</c> if [refresh on version change]; otherwise, <c>false</c>.</value>
        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Supports the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return false;
            var trailer = item as Trailer;

            if (trailer != null)
            {
                return !trailer.IsLocalTrailer;
            }

            // Don't support local trailers
            return item is Movie;
        }

        /// <summary>
        /// Gets the comparison data.
        /// </summary>
        /// <param name="imdbId">The imdb id.</param>
        /// <returns>Guid.</returns>
        private Guid GetComparisonData(string imdbId)
        {
            return string.IsNullOrEmpty(imdbId) ? Guid.Empty : imdbId.GetMD5();
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get
            {
                // Run after moviedb and xml providers
                return MetadataProviderPriority.Last;
            }
        }

        /// <summary>
        /// Needses the refresh internal.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            // Refresh if rt id has changed
            if (providerInfo.Data != GetComparisonData(item.GetProviderId(MetadataProviders.Imdb)))
            {
                return true;
            }

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            BaseProviderInfo data;

            if (!item.ProviderData.TryGetValue(Id, out data))
            {
                data = new BaseProviderInfo();
                item.ProviderData[Id] = data;
            }

            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            if (string.IsNullOrEmpty(imdbId))
            {
                data.Data = GetComparisonData(imdbId);
                data.LastRefreshStatus = ProviderRefreshStatus.Success;
                return true;
            }

            await FetchRottenTomatoesId(item, cancellationToken).ConfigureAwait(false);

            using (var stream = await HttpClient.Get(new HttpRequestOptions
            {
                Url = GetMovieReviewsUrl(item.GetProviderId(MetadataProviders.RottenTomatoes)),
                ResourcePool = _rottenTomatoesResourcePool,
                CancellationToken = cancellationToken,
                EnableResponseCache = true

            }).ConfigureAwait(false))
            {

                var result = JsonSerializer.DeserializeFromStream<RTReviewList>(stream);

                item.CriticReviews = result.reviews.Select(rtReview => new ItemReview
                {
                    ReviewerName = rtReview.critic,
                    Publisher = rtReview.publication,
                    Date = DateTime.Parse(rtReview.date).ToUniversalTime(),
                    Caption = rtReview.quote,
                    Url = rtReview.links.review,
                    Likes = string.Equals(rtReview.freshness, "fresh", StringComparison.OrdinalIgnoreCase)

                }).ToList();
            }

            data.Data = GetComparisonData(item.GetProviderId(MetadataProviders.Imdb));
            data.LastRefreshStatus = ProviderRefreshStatus.Success;
            SetLastRefreshed(item, DateTime.UtcNow);

            return true;
        }

        /// <summary>
        /// Fetches the rotten tomatoes id.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchRottenTomatoesId(BaseItem item, CancellationToken cancellationToken)
        {
            var imdbId = item.GetProviderId(MetadataProviders.Imdb);

            // Have IMDB Id
            using (var stream = await HttpClient.Get(new HttpRequestOptions
            {
                Url = GetMovieImdbUrl(imdbId),
                ResourcePool = _rottenTomatoesResourcePool,
                CancellationToken = cancellationToken,
                EnableResponseCache = true

            }).ConfigureAwait(false))
            {
                var hit = JsonSerializer.DeserializeFromStream<RTMovieSearchResult>(stream);

                if (!string.IsNullOrEmpty(hit.id))
                {
                    // Got a result
                    item.CriticRatingSummary = hit.critics_consensus;
                    item.CriticRating = float.Parse(hit.ratings.critics_score);

                    item.SetProviderId(MetadataProviders.RottenTomatoes, hit.id);
                }
            }
        }


        // Utility functions to get the URL of the API calls

        private string GetMovieReviewsUrl(string rtId)
        {
            return BasicUrl + string.Format(MoviesReviews, ApiKey, rtId);
        }
        private string GetMovieImdbUrl(string imdbId)
        {
            return BasicUrl + string.Format(MovieImdb, ApiKey, imdbId.TrimStart('t'));
        }

        // Data contract classes for use with the Rotten Tomatoes API

        protected class RTReviewList
        {
            public int total { get; set; }
            public List<RTReview> reviews { get; set; }
        }

        protected class RTReview
        {
            public string critic { get; set; }
            public string date { get; set; }
            public string freshness { get; set; }
            public string publication { get; set; }
            public string quote { get; set; }
            public RTReviewLink links { get; set; }
            public string original_score { get; set; }
        }

        protected class RTReviewLink
        {
            public string review { get; set; }
        }

        protected class RTSearchResults
        {
            public int total { get; set; }
            public List<RTMovieSearchResult> movies { get; set; }
            public RTSearchLinks links { get; set; }
            public string link_template { get; set; }
        }

        protected class RTSearchLinks
        {
            public string self { get; set; }
            public string next { get; set; }
            public string previous { get; set; }
        }

        protected class RTMovieSearchResult
        {
            public string title { get; set; }
            public int year { get; set; }
            public string runtime { get; set; }
            public string synopsis { get; set; }
            public string critics_consensus { get; set; }
            public string mpaa_rating { get; set; }
            public string id { get; set; }
            public RTRatings ratings { get; set; }
            public RTAlternateIds alternate_ids { get; set; }
        }

        protected class RTRatings
        {
            public string critics_rating { get; set; }
            public string critics_score { get; set; }
        }

        protected class RTAlternateIds
        {
            public string imdb { get; set; }
        }

    }
}