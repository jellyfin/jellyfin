using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
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
        private const string Search3 = @"http://api.themoviedb.org/3/search/{3}?api_key={1}&query={0}&language={2}";

        internal static string ApiKey = "f6bd687ffa63cd282b6ff2c6877f2669";
        internal static string AcceptHeader = "application/json,image/*";
        
        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;

        public MovieDbSearch(ILogger logger, IJsonSerializer json)
        {
            _logger = logger;
            _json = json;
        }

        public Task<string> FindMovieId(ItemLookupInfo idInfo, CancellationToken cancellationToken)
        {
            return FindId(idInfo, "movie", cancellationToken);
        }

        public Task<string> FindCollectionId(ItemLookupInfo idInfo, CancellationToken cancellationToken)
        {
            return FindId(idInfo, "collection", cancellationToken);
        }

        private async Task<string> FindId(ItemLookupInfo idInfo, string searchType, CancellationToken cancellationToken)
        {
            int? yearInName;
            var name = idInfo.Name;
            NameParser.ParseName(name, out name, out yearInName);

            var year = idInfo.Year ?? yearInName;

            _logger.Info("MovieDbProvider: Finding id for item: " + name);
            var language = idInfo.MetadataLanguage.ToLower();

            //nope - search for it
            //var searchType = item is BoxSet ? "collection" : "movie";

            var id = await AttemptFindId(name, searchType, year, language, cancellationToken).ConfigureAwait(false);
            
            if (id == null)
            {
                //try in english if wasn't before
                if (language != "en")
                {
                    id = await AttemptFindId(name, searchType, year, "en", cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // try with dot and _ turned to space
                    var originalName = name;

                    name = name.Replace(",", " ");
                    name = name.Replace(".", " ");
                    name = name.Replace("_", " ");
                    name = name.Replace("-", " ");

                    // Search again if the new name is different
                    if (!string.Equals(name, originalName))
                    {
                        id = await AttemptFindId(name, searchType, year, language, cancellationToken).ConfigureAwait(false);

                        if (id == null && language != "en")
                        {
                            //one more time, in english
                            id = await AttemptFindId(name, searchType, year, "en", cancellationToken).ConfigureAwait(false);

                        }
                    }
                }
            }

            return id;
        }

        private async Task<string> AttemptFindId(string name, string type, int? year, string language, CancellationToken cancellationToken)
        {
            var url3 = string.Format(Search3, WebUtility.UrlEncode(name), ApiKey, language, type);

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url3,
                CancellationToken = cancellationToken,
                AcceptHeader = AcceptHeader

            }).ConfigureAwait(false))
            {
                var searchResult = _json.DeserializeFromStream<TmdbMovieSearchResults>(json);
                return FindIdOfBestResult(searchResult.results, name, year);
            }
        }

        private string FindIdOfBestResult(List<TmdbMovieSearchResult> results, string name, int? year)
        {
            if (year.HasValue)
            {
                // Take the first result from the same year
                var id = results.Where(i =>
                {
                    // Make sure it has a name
                    if (!string.IsNullOrEmpty(i.title ?? i.name))
                    {
                        DateTime r;

                        // These dates are always in this exact format
                        if (DateTime.TryParseExact(i.release_date, "yyyy-MM-dd", EnUs, DateTimeStyles.None, out r))
                        {
                            return r.Year == year.Value;
                        }
                    }

                    return false;
                })
                    .Select(i => i.id.ToString(CultureInfo.InvariantCulture))
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }

                // Take the first result within one year
                id = results.Where(i =>
                {
                    // Make sure it has a name
                    if (!string.IsNullOrEmpty(i.title ?? i.name))
                    {
                        DateTime r;

                        // These dates are always in this exact format
                        if (DateTime.TryParseExact(i.release_date, "yyyy-MM-dd", EnUs, DateTimeStyles.None, out r))
                        {
                            return Math.Abs(r.Year - year.Value) <= 1;
                        }
                    }

                    return false;
                })
                   .Select(i => i.id.ToString(CultureInfo.InvariantCulture))
                   .FirstOrDefault();

                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
            }

            // Just take the first one
            return results.Where(i => !string.IsNullOrEmpty(i.title ?? i.name))
                .Select(i => i.id.ToString(CultureInfo.InvariantCulture))
                .FirstOrDefault();
        }


        /// <summary>
        /// Class TmdbMovieSearchResult
        /// </summary>
        private class TmdbMovieSearchResult
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

    }
}
