using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    public class MovieDbSearch
    {
        private static readonly CultureInfo EnUs = new CultureInfo("en-US");
        private const string Search3 = @"https://api.themoviedb.org/3/search/{3}?api_key={1}&query={0}&language={2}";

        internal static string ApiKey = "f6bd687ffa63cd282b6ff2c6877f2669";
        internal static string AcceptHeader = "application/json,image/*";

        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;
        private readonly ILibraryManager _libraryManager;

        public MovieDbSearch(ILogger logger, IJsonSerializer json, ILibraryManager libraryManager)
        {
            _logger = logger;
            _json = json;
            _libraryManager = libraryManager;
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo idInfo, CancellationToken cancellationToken)
        {
            return GetSearchResults(idInfo, "tv", cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetMovieSearchResults(ItemLookupInfo idInfo, CancellationToken cancellationToken)
        {
            return GetSearchResults(idInfo, "movie", cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BoxSetInfo idInfo, CancellationToken cancellationToken)
        {
            return GetSearchResults(idInfo, "collection", cancellationToken);
        }

        private async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(ItemLookupInfo idInfo, string searchType, CancellationToken cancellationToken)
        {
            var name = idInfo.Name;
            var year = idInfo.Year;

            if (string.IsNullOrWhiteSpace(name))
            {
                return new List<RemoteSearchResult>();
            }

            var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var tmdbImageUrl = tmdbSettings.images.secure_base_url + "original";

            if (!string.IsNullOrWhiteSpace(name))
            {
                var parsedName = _libraryManager.ParseName(name);
                var yearInName = parsedName.Year;
                name = parsedName.Name;
                year = year ?? yearInName;
            }

            _logger.Info("MovieDbProvider: Finding id for item: " + name);
            var language = idInfo.MetadataLanguage.ToLower();

            //nope - search for it
            //var searchType = item is BoxSet ? "collection" : "movie";

            var results = await GetSearchResults(name, searchType, year, language, tmdbImageUrl, cancellationToken).ConfigureAwait(false);

            if (results.Count == 0)
            {
                //try in english if wasn't before
                if (!string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
                {
                    results = await GetSearchResults(name, searchType, year, "en", tmdbImageUrl, cancellationToken).ConfigureAwait(false);
                }
            }

            if (results.Count == 0)
            {
                // try with dot and _ turned to space
                var originalName = name;

                name = name.Replace(",", " ");
                name = name.Replace(".", " ");
                name = name.Replace("_", " ");
                name = name.Replace("-", " ");
                name = name.Replace("!", " ");
                name = name.Replace("?", " ");

                name = name.Trim();

                // Search again if the new name is different
                if (!string.Equals(name, originalName))
                {
                    results = await GetSearchResults(name, searchType, year, language, tmdbImageUrl, cancellationToken).ConfigureAwait(false);

                    if (results.Count == 0 && !string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
                    {
                        //one more time, in english
                        results = await GetSearchResults(name, searchType, year, "en", tmdbImageUrl, cancellationToken).ConfigureAwait(false);

                    }
                }
            }

            return results.Where(i =>
            {
                if (year.HasValue && i.ProductionYear.HasValue)
                {
                    // Allow one year tolerance
                    return Math.Abs(year.Value - i.ProductionYear.Value) <= 1;
                }

                return true;
            });
        }

        private Task<List<RemoteSearchResult>> GetSearchResults(string name, string type, int? year, string language, string baseImageUrl, CancellationToken cancellationToken)
        {
            switch (type)
            {
                case "tv":
                    return GetSearchResultsTv(name, year, language, baseImageUrl, cancellationToken);
                default:
                    return GetSearchResultsGeneric(name, type, year, language, baseImageUrl, cancellationToken);
            }
        }

        private async Task<List<RemoteSearchResult>> GetSearchResultsGeneric(string name, string type, int? year, string language, string baseImageUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name");
            }

            var url3 = string.Format(Search3, WebUtility.UrlEncode(name), ApiKey, language, type);

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url3,
                CancellationToken = cancellationToken,
                AcceptHeader = AcceptHeader

            }).ConfigureAwait(false))
            {
                var searchResults = _json.DeserializeFromStream<TmdbMovieSearchResults>(json);

                var results = searchResults.results ?? new List<TmdbMovieSearchResult>();

                var index = 0;
                var resultTuples = results.Select(result => new Tuple<TmdbMovieSearchResult, int>(result, index++)).ToList();

                return resultTuples.OrderBy(i => GetSearchResultOrder(i.Item1, year))
                    .ThenBy(i => i.Item2)
                    .Select(i => i.Item1)
                    .Select(i =>
                    {
                        var remoteResult = new RemoteSearchResult
                        {
                            SearchProviderName = MovieDbProvider.Current.Name,
                            Name = i.title ?? i.name ?? i.original_title,
                            ImageUrl = string.IsNullOrWhiteSpace(i.poster_path) ? null : baseImageUrl + i.poster_path
                        };

                        if (!string.IsNullOrWhiteSpace(i.release_date))
                        {
                            DateTime r;

                            // These dates are always in this exact format
                            if (DateTime.TryParseExact(i.release_date, "yyyy-MM-dd", EnUs, DateTimeStyles.None, out r))
                            {
                                remoteResult.PremiereDate = r.ToUniversalTime();
                                remoteResult.ProductionYear = remoteResult.PremiereDate.Value.Year;
                            }
                        }

                        remoteResult.SetProviderId(MetadataProviders.Tmdb, i.id.ToString(EnUs));

                        return remoteResult;

                    })
                    .ToList();
            }
        }

        private async Task<List<RemoteSearchResult>> GetSearchResultsTv(string name, int? year, string language, string baseImageUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name");
            }

            var url3 = string.Format(Search3, WebUtility.UrlEncode(name), ApiKey, language, "tv");

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url3,
                CancellationToken = cancellationToken,
                AcceptHeader = AcceptHeader

            }).ConfigureAwait(false))
            {
                var searchResults = _json.DeserializeFromStream<TmdbTvSearchResults>(json);

                var results = searchResults.results ?? new List<TvResult>();

                var index = 0;
                var resultTuples = results.Select(result => new Tuple<TvResult, int>(result, index++)).ToList();

                return resultTuples.OrderBy(i => GetSearchResultOrder(i.Item1, year))
                    .ThenBy(i => i.Item2)
                    .Select(i => i.Item1)
                    .Select(i =>
                    {
                        var remoteResult = new RemoteSearchResult
                        {
                            SearchProviderName = MovieDbProvider.Current.Name,
                            Name = i.name ?? i.original_name,
                            ImageUrl = string.IsNullOrWhiteSpace(i.poster_path) ? null : baseImageUrl + i.poster_path
                        };

                        if (!string.IsNullOrWhiteSpace(i.first_air_date))
                        {
                            DateTime r;

                            // These dates are always in this exact format
                            if (DateTime.TryParseExact(i.first_air_date, "yyyy-MM-dd", EnUs, DateTimeStyles.None, out r))
                            {
                                remoteResult.PremiereDate = r.ToUniversalTime();
                                remoteResult.ProductionYear = remoteResult.PremiereDate.Value.Year;
                            }
                        }

                        remoteResult.SetProviderId(MetadataProviders.Tmdb, i.id.ToString(EnUs));

                        return remoteResult;

                    })
                    .ToList();
            }
        }

        private int GetSearchResultOrder(TmdbMovieSearchResult result, int? year)
        {
            if (year.HasValue)
            {
                DateTime r;

                // These dates are always in this exact format
                if (DateTime.TryParseExact(result.release_date, "yyyy-MM-dd", EnUs, DateTimeStyles.None, out r))
                {
                    // Allow one year tolernace, preserve order from Tmdb
                    return Math.Abs(r.Year - year.Value);
                }
            }

            return int.MaxValue;
        }

        private int GetSearchResultOrder(TvResult result, int? year)
        {
            if (year.HasValue)
            {
                DateTime r;

                // These dates are always in this exact format
                if (DateTime.TryParseExact(result.first_air_date, "yyyy-MM-dd", EnUs, DateTimeStyles.None, out r))
                {
                    // Allow one year tolernace, preserve order from Tmdb
                    return Math.Abs(r.Year - year.Value);
                }
            }

            return int.MaxValue;
        }

        /// <summary>
        /// Class TmdbMovieSearchResult
        /// </summary>
        public class TmdbMovieSearchResult
        {
            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="TmdbMovieSearchResult" /> is adult.
            /// </summary>
            /// <value><c>true</c> if adult; otherwise, <c>false</c>.</value>
            public bool adult { get; set; }
            /// <summary>
            /// Gets or sets the backdrop_path.
            /// </summary>
            /// <value>The backdrop_path.</value>
            public string backdrop_path { get; set; }
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int id { get; set; }
            /// <summary>
            /// Gets or sets the original_title.
            /// </summary>
            /// <value>The original_title.</value>
            public string original_title { get; set; }
            /// <summary>
            /// Gets or sets the original_name.
            /// </summary>
            /// <value>The original_name.</value>
            public string original_name { get; set; }
            /// <summary>
            /// Gets or sets the release_date.
            /// </summary>
            /// <value>The release_date.</value>
            public string release_date { get; set; }
            /// <summary>
            /// Gets or sets the poster_path.
            /// </summary>
            /// <value>The poster_path.</value>
            public string poster_path { get; set; }
            /// <summary>
            /// Gets or sets the popularity.
            /// </summary>
            /// <value>The popularity.</value>
            public double popularity { get; set; }
            /// <summary>
            /// Gets or sets the title.
            /// </summary>
            /// <value>The title.</value>
            public string title { get; set; }
            /// <summary>
            /// Gets or sets the vote_average.
            /// </summary>
            /// <value>The vote_average.</value>
            public double vote_average { get; set; }
            /// <summary>
            /// For collection search results
            /// </summary>
            public string name { get; set; }
            /// <summary>
            /// Gets or sets the vote_count.
            /// </summary>
            /// <value>The vote_count.</value>
            public int vote_count { get; set; }
        }

        /// <summary>
        /// Class TmdbMovieSearchResults
        /// </summary>
        private class TmdbMovieSearchResults
        {
            /// <summary>
            /// Gets or sets the page.
            /// </summary>
            /// <value>The page.</value>
            public int page { get; set; }
            /// <summary>
            /// Gets or sets the results.
            /// </summary>
            /// <value>The results.</value>
            public List<TmdbMovieSearchResult> results { get; set; }
            /// <summary>
            /// Gets or sets the total_pages.
            /// </summary>
            /// <value>The total_pages.</value>
            public int total_pages { get; set; }
            /// <summary>
            /// Gets or sets the total_results.
            /// </summary>
            /// <value>The total_results.</value>
            public int total_results { get; set; }
        }

        public class TvResult
        {
            public string backdrop_path { get; set; }
            public string first_air_date { get; set; }
            public int id { get; set; }
            public string original_name { get; set; }
            public string poster_path { get; set; }
            public double popularity { get; set; }
            public string name { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
        }

        /// <summary>
        /// Class TmdbTvSearchResults
        /// </summary>
        private class TmdbTvSearchResults
        {
            /// <summary>
            /// Gets or sets the page.
            /// </summary>
            /// <value>The page.</value>
            public int page { get; set; }
            /// <summary>
            /// Gets or sets the results.
            /// </summary>
            /// <value>The results.</value>
            public List<TvResult> results { get; set; }
            /// <summary>
            /// Gets or sets the total_pages.
            /// </summary>
            /// <value>The total_pages.</value>
            public int total_pages { get; set; }
            /// <summary>
            /// Gets or sets the total_results.
            /// </summary>
            /// <value>The total_results.</value>
            public int total_results { get; set; }
        }

        public class ExternalIdLookupResult
        {
            public List<object> movie_results { get; set; }
            public List<object> person_results { get; set; }
            public List<TvResult> tv_results { get; set; }
        }
    }
}
