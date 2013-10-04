using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    public class OpenMovieDatabaseProvider : BaseMetadataProvider
    {
        private readonly SemaphoreSlim _resourcePool = new SemaphoreSlim(1, 1);

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

        public OpenMovieDatabaseProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IHttpClient httpClient)
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
                return "11";
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
            var trailer = item as Trailer;

            // Don't support local trailers
            if (trailer != null)
            {
                return !trailer.IsLocalTrailer;
            }

            return item is Movie || item is MusicVideo || item is Series;
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
                return MetadataProviderPriority.Fifth;
            }
        }

        protected readonly CultureInfo UsCulture = new CultureInfo("en-US");

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
                data.LastRefreshStatus = ProviderRefreshStatus.Success;
                return true;
            }

            var imdbParam = imdbId.StartsWith("tt", StringComparison.OrdinalIgnoreCase) ? imdbId : "tt" + imdbId;

            var url = string.Format("http://www.omdbapi.com/?i={0}&tomatoes=true", imdbParam);

            using (var stream = await HttpClient.Get(new HttpRequestOptions
            {
                Url = url,
                ResourcePool = _resourcePool,
                CancellationToken = cancellationToken

            }).ConfigureAwait(false))
            {
                var result = JsonSerializer.DeserializeFromStream<RootObject>(stream);

                // Seeing some bogus RT data on omdb for series, so filter it out here
                // RT doesn't even have tv series
                int tomatoMeter;

                if (!string.IsNullOrEmpty(result.tomatoMeter)
                    && int.TryParse(result.tomatoMeter, NumberStyles.Integer, UsCulture, out tomatoMeter)
                    && tomatoMeter >= 0)
                {
                    item.CriticRating = tomatoMeter;
                }

                if (!string.IsNullOrEmpty(result.tomatoConsensus)
                    && !string.Equals(result.tomatoConsensus, "n/a", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(result.tomatoConsensus, "No consensus yet.", StringComparison.OrdinalIgnoreCase))
                {
                    item.CriticRatingSummary = result.tomatoConsensus;
                }

                int voteCount;

                if (!string.IsNullOrEmpty(result.imdbVotes)
                    && int.TryParse(result.imdbVotes, NumberStyles.Integer, UsCulture, out voteCount)
                    && voteCount >= 0)
                {
                    item.VoteCount = voteCount;
                }

                float imdbRating;

                if (!string.IsNullOrEmpty(result.imdbRating)
                    && float.TryParse(result.imdbRating, NumberStyles.Number, UsCulture, out imdbRating)
                    && imdbRating >= 0)
                {
                    item.CommunityRating = imdbRating;
                }

                ParseAdditionalMetadata(item, result);
            }

            data.LastRefreshStatus = ProviderRefreshStatus.Success;
            SetLastRefreshed(item, DateTime.UtcNow);

            return true;
        }

        private void ParseAdditionalMetadata(BaseItem item, RootObject result)
        {
            // Grab series genres because imdb data is better than tvdb. Leave movies alone
            // But only do it if english is the preferred language because this data will not be localized
            if (!item.LockedFields.Contains(MetadataFields.Genres) &&
                ShouldFetchGenres(item) &&
                !string.IsNullOrWhiteSpace(result.Genre) &&
                !string.Equals(result.Genre, "n/a", StringComparison.OrdinalIgnoreCase))
            {
                item.Genres.Clear();

                foreach (var genre in result.Genre
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(i => i.Trim())
                    .Where(i => !string.IsNullOrWhiteSpace(i)))
                {
                    item.AddGenre(genre);
                }
            }
        }

        private bool ShouldFetchGenres(BaseItem item)
        {
            // Only fetch if other providers didn't get anything
            if (item is Trailer)
            {
                return item.Genres.Count == 0;
            }

            return item is Series;
        }

        protected class RootObject
        {
            public string Title { get; set; }
            public string Year { get; set; }
            public string Rated { get; set; }
            public string Released { get; set; }
            public string Runtime { get; set; }
            public string Genre { get; set; }
            public string Director { get; set; }
            public string Writer { get; set; }
            public string Actors { get; set; }
            public string Plot { get; set; }
            public string Poster { get; set; }
            public string imdbRating { get; set; }
            public string imdbVotes { get; set; }
            public string imdbID { get; set; }
            public string Type { get; set; }
            public string tomatoMeter { get; set; }
            public string tomatoImage { get; set; }
            public string tomatoRating { get; set; }
            public string tomatoReviews { get; set; }
            public string tomatoFresh { get; set; }
            public string tomatoRotten { get; set; }
            public string tomatoConsensus { get; set; }
            public string tomatoUserMeter { get; set; }
            public string tomatoUserRating { get; set; }
            public string tomatoUserReviews { get; set; }
            public string DVD { get; set; }
            public string BoxOffice { get; set; }
            public string Production { get; set; }
            public string Website { get; set; }
            public string Response { get; set; }
        }
    }
}
