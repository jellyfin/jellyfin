using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Providers.Savers;

namespace MediaBrowser.Providers.Movies
{
    /// <summary>
    /// Class MovieDbProvider
    /// </summary>
    public class MovieDbProvider : BaseMetadataProvider, IDisposable
    {
        protected static CultureInfo EnUs = new CultureInfo("en-US");

        protected readonly IProviderManager ProviderManager;

        /// <summary>
        /// The movie db
        /// </summary>
        private readonly SemaphoreSlim _movieDbResourcePool = new SemaphoreSlim(1, 1);

        internal static MovieDbProvider Current { get; private set; }

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
        /// Initializes a new instance of the <see cref="MovieDbProvider" /> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="providerManager">The provider manager.</param>
        public MovieDbProvider(ILogManager logManager, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IHttpClient httpClient, IProviderManager providerManager)
            : base(logManager, configurationManager)
        {
            JsonSerializer = jsonSerializer;
            HttpClient = httpClient;
            ProviderManager = providerManager;
            Current = this;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _movieDbResourcePool.Dispose();
            }
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Third; }
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            var trailer = item as Trailer;

            if (trailer != null)
            {
                return !trailer.IsLocalTrailer;
            }

            // Don't support local trailers
            return item is Movie || item is BoxSet || item is MusicVideo;
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

        protected override bool RefreshOnVersionChange
        {
            get
            {
                return true;
            }
        }

        protected override string ProviderVersion
        {
            get
            {
                return "2";
            }
        }

        /// <summary>
        /// The _TMDB settings task
        /// </summary>
        private TmdbSettingsResult _tmdbSettings;

        private readonly SemaphoreSlim _tmdbSettingsSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Gets the TMDB settings.
        /// </summary>
        /// <returns>Task{TmdbSettingsResult}.</returns>
        internal async Task<TmdbSettingsResult> GetTmdbSettings(CancellationToken cancellationToken)
        {
            if (_tmdbSettings != null)
            {
                return _tmdbSettings;
            }

            await _tmdbSettingsSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Check again in case it got populated while we were waiting.
                if (_tmdbSettings != null)
                {
                    return _tmdbSettings;
                }

                using (var json = await GetMovieDbResponse(new HttpRequestOptions
                {
                    Url = string.Format(TmdbConfigUrl, ApiKey),
                    CancellationToken = cancellationToken,
                    AcceptHeader = AcceptHeader

                }).ConfigureAwait(false))
                {
                    _tmdbSettings = JsonSerializer.DeserializeFromStream<TmdbSettingsResult>(json);

                    return _tmdbSettings;
                }
            }
            finally
            {
                _tmdbSettingsSemaphore.Release();
            }
        }

        private const string TmdbConfigUrl = "http://api.themoviedb.org/3/configuration?api_key={0}";
        private const string Search3 = @"http://api.themoviedb.org/3/search/{3}?api_key={1}&query={0}&language={2}";
        private const string GetMovieInfo3 = @"http://api.themoviedb.org/3/movie/{0}?api_key={1}&language={2}&append_to_response=casts,releases,images,keywords,trailers";
        private const string GetBoxSetInfo3 = @"http://api.themoviedb.org/3/collection/{0}?api_key={1}&language={2}&append_to_response=images";

        internal static string ApiKey = "f6bd687ffa63cd282b6ff2c6877f2669";
        internal static string AcceptHeader = "application/json,image/*";

        static readonly Regex[] NameMatches = new[] {
            new Regex(@"(?<name>.*)\((?<year>\d{4})\)"), // matches "My Movie (2001)" and gives us the name and the year
            new Regex(@"(?<name>.*)") // last resort matches the whole string as the name
        };

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (HasAltMeta(item))
                return false;

            // Boxsets require two passes because we need the children to be refreshed
            if (item is BoxSet && string.IsNullOrEmpty(item.GetProviderId(MetadataProviders.TmdbCollection)))
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
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await FetchMovieData(item, cancellationToken).ConfigureAwait(false);

            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Determines whether [has alt meta] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [has alt meta] [the specified item]; otherwise, <c>false</c>.</returns>
        internal static bool HasAltMeta(BaseItem item)
        {
            if (item is BoxSet)
            {
                return item.LocationType == LocationType.FileSystem && item.ResolveArgs.ContainsMetaFileByName("collection.xml");
            }

            var path = MovieXmlSaver.GetMovieSavePath(item);

            if (item.LocationType == LocationType.FileSystem)
            {
                // If mixed with multiple movies in one folder, resolve args won't have the file system children
                return item.ResolveArgs.ContainsMetaFileByName(Path.GetFileName(path)) || File.Exists(path);
            }

            return false;
        }

        /// <summary>
        /// Fetches the movie data.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Task.</returns>
        private async Task FetchMovieData(BaseItem item, CancellationToken cancellationToken)
        {
            var id = item.GetProviderId(MetadataProviders.Tmdb);

            if (string.IsNullOrEmpty(id))
            {
                id = item.GetProviderId(MetadataProviders.Imdb);
            }

            if (string.IsNullOrEmpty(id))
            {
                id = await FindId(item, cancellationToken).ConfigureAwait(false);
            }

            if (!string.IsNullOrEmpty(id))
            {
                Logger.Debug("MovieDbProvider - getting movie info with id: " + id);

                cancellationToken.ThrowIfCancellationRequested();

                await FetchMovieData(item, id, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Logger.Info("MovieDBProvider could not find " + item.Name + ". Check name on themoviedb.org.");
            }
        }

        /// <summary>
        /// Parses the name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="justName">Name of the just.</param>
        /// <param name="year">The year.</param>
        protected void ParseName(string name, out string justName, out int? year)
        {
            justName = null;
            year = null;
            foreach (var re in NameMatches)
            {
                Match m = re.Match(name);
                if (m.Success)
                {
                    justName = m.Groups["name"].Value.Trim();
                    string y = m.Groups["year"] != null ? m.Groups["year"].Value : null;
                    int temp;
                    year = Int32.TryParse(y, out temp) ? temp : (int?)null;
                    break;
                }
            }
        }

        /// <summary>
        /// Finds the id.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task{System.String}.</returns>
        public async Task<string> FindId(BaseItem item, CancellationToken cancellationToken)
        {
            int? yearInName;
            string name = item.Name;
            ParseName(name, out name, out yearInName);

            var year = item.ProductionYear ?? yearInName;

            Logger.Info("MovieDbProvider: Finding id for item: " + name);
            string language = ConfigurationManager.Configuration.PreferredMetadataLanguage.ToLower();

            //if we are a boxset - look at our first child
            var boxset = item as BoxSet;
            if (boxset != null)
            {
                // See if any movies have a collection id already
                var collId = boxset.Children.Concat(boxset.GetLinkedChildren()).OfType<Video>()
                    .Select(i => i.GetProviderId(MetadataProviders.TmdbCollection))
                   .FirstOrDefault(i => i != null);

                if (collId != null) return collId;

            }

            //nope - search for it
            var searchType = item is BoxSet ? "collection" : "movie";
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
                    name = name.Replace("-", "");

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

                    if (id == null && item.LocationType == LocationType.FileSystem)
                    {
                        //last resort - try using the actual folder name
                        var pathName = Path.GetFileName(item.ResolveArgs.Path);

                        // Only search if it's a name we haven't already tried.
                        if (!string.Equals(pathName, name, StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(pathName, originalName, StringComparison.OrdinalIgnoreCase))
                        {
                            id = await AttemptFindId(pathName, searchType, year, "en", cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }

            return id;
        }

        /// <summary>
        /// Attempts the find id.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="type">movie or collection</param>
        /// <param name="year">The year.</param>
        /// <param name="language">The language.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> AttemptFindId(string name, string type, int? year, string language, CancellationToken cancellationToken)
        {
            string url3 = string.Format(Search3, UrlEncode(name), ApiKey, language, type);
            TmdbMovieSearchResults searchResult = null;

            using (Stream json = await GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url3,
                CancellationToken = cancellationToken,
                AcceptHeader = AcceptHeader

            }).ConfigureAwait(false))
            {
                searchResult = JsonSerializer.DeserializeFromStream<TmdbMovieSearchResults>(json);
            }

            if (searchResult != null)
            {
                foreach (var possible in searchResult.results)
                {
                    string matchedName = possible.title ?? possible.name;
                    string id = possible.id.ToString(CultureInfo.InvariantCulture);

                    if (matchedName != null)
                    {
                        Logger.Debug("Match " + matchedName + " for " + name);
                        if (year != null)
                        {
                            DateTime r;

                            //These dates are always in this exact format
                            if (DateTime.TryParseExact(possible.release_date, "yyyy-MM-dd", EnUs, DateTimeStyles.None, out r))
                            {
                                if (Math.Abs(r.Year - year.Value) > 1) // allow a 1 year tolerance on release date
                                {
                                    Logger.Debug("Result " + matchedName + " released on " + r + " did not match year " + year);
                                    continue;
                                }
                            }
                        }
                        //matched name and year
                        return id;
                    }

                }
            }

            return null;
        }

        /// <summary>
        /// URLs the encode.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>System.String.</returns>
        private static string UrlEncode(string name)
        {
            return WebUtility.UrlEncode(name);
        }

        /// <summary>
        /// Fetches the movie data.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="id">The id.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task.</returns>
        protected async Task FetchMovieData(BaseItem item, string id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (String.IsNullOrEmpty(id))
            {
                Logger.Info("MoviedbProvider: Ignoring " + item.Name + " because ID forced blank.");
                return;
            }

            item.SetProviderId(MetadataProviders.Tmdb, id);

            var mainResult = await FetchMainResult(item, id, cancellationToken).ConfigureAwait(false);

            if (mainResult == null) return;

            ProcessMainInfo(item, mainResult);
        }

        /// <summary>
        /// Fetches the main result.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="id">The id.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns>Task{CompleteMovieData}.</returns>
        protected async Task<CompleteMovieData> FetchMainResult(BaseItem item, string id, CancellationToken cancellationToken)
        {
            var baseUrl = item is BoxSet ? GetBoxSetInfo3 : GetMovieInfo3;

            string url = string.Format(baseUrl, id, ApiKey, ConfigurationManager.Configuration.PreferredMetadataLanguage);
            CompleteMovieData mainResult;

            cancellationToken.ThrowIfCancellationRequested();

            using (var json = await GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = AcceptHeader

            }).ConfigureAwait(false))
            {
                mainResult = JsonSerializer.DeserializeFromStream<CompleteMovieData>(json);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (mainResult != null && string.IsNullOrEmpty(mainResult.overview))
            {
                if (ConfigurationManager.Configuration.PreferredMetadataLanguage.ToLower() != "en")
                {
                    Logger.Info("MovieDbProvider couldn't find meta for language " + ConfigurationManager.Configuration.PreferredMetadataLanguage + ". Trying English...");

                    url = string.Format(baseUrl, id, ApiKey, "en");

                    using (Stream json = await GetMovieDbResponse(new HttpRequestOptions
                    {
                        Url = url,
                        CancellationToken = cancellationToken,
                        AcceptHeader = AcceptHeader

                    }).ConfigureAwait(false))
                    {
                        mainResult = JsonSerializer.DeserializeFromStream<CompleteMovieData>(json);
                    }

                    if (String.IsNullOrEmpty(mainResult.overview))
                    {
                        Logger.Error("MovieDbProvider - Unable to find information for " + item.Name + " (id:" + id + ")");
                        return null;
                    }
                }
            }
            return mainResult;
        }

        /// <summary>
        /// Processes the main info.
        /// </summary>
        /// <param name="movie">The movie.</param>
        /// <param name="movieData">The movie data.</param>
        protected virtual void ProcessMainInfo(BaseItem movie, CompleteMovieData movieData)
        {
            if (movie != null && movieData != null)
            {
                if (!movie.LockedFields.Contains(MetadataFields.Name))
                {
                    movie.Name = movieData.title ?? movieData.original_title ?? movieData.name ?? movie.Name;
                }
                if (!movie.LockedFields.Contains(MetadataFields.Overview))
                {
                    movie.Overview = WebUtility.HtmlDecode(movieData.overview);
                    movie.Overview = movie.Overview != null ? movie.Overview.Replace("\n\n", "\n") : null;
                }
                movie.HomePageUrl = movieData.homepage;

                movie.Budget = movieData.budget;
                movie.Revenue = movieData.revenue;

                if (!string.IsNullOrEmpty(movieData.tagline))
                {
                    movie.Taglines.Clear();
                    movie.AddTagline(movieData.tagline);
                }

                movie.SetProviderId(MetadataProviders.Imdb, movieData.imdb_id);

                if (movieData.belongs_to_collection != null)
                {
                    movie.SetProviderId(MetadataProviders.TmdbCollection, movieData.belongs_to_collection.id.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    movie.SetProviderId(MetadataProviders.TmdbCollection, null); // clear out any old entry
                }

                float rating;
                string voteAvg = movieData.vote_average.ToString(CultureInfo.InvariantCulture);

                //tmdb appears to have unified their numbers to always report "7.3" regardless of country
                // so I removed the culture-specific processing here because it was not working for other countries -ebr
                if (float.TryParse(voteAvg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out rating))
                    movie.CommunityRating = rating;

                movie.VoteCount = movieData.vote_count;

                //release date and certification are retrieved based on configured country and we fall back on US if not there and to minimun release date if still no match
                if (movieData.releases != null && movieData.releases.countries != null)
                {
                    var ourRelease = movieData.releases.countries.FirstOrDefault(c => c.iso_3166_1.Equals(ConfigurationManager.Configuration.MetadataCountryCode, StringComparison.OrdinalIgnoreCase)) ?? new Country();
                    var usRelease = movieData.releases.countries.FirstOrDefault(c => c.iso_3166_1.Equals("US", StringComparison.OrdinalIgnoreCase)) ?? new Country();
                    var minimunRelease = movieData.releases.countries.OrderBy(c => c.release_date).FirstOrDefault() ?? new Country();

                    if (!movie.LockedFields.Contains(MetadataFields.OfficialRating))
                    {
                        var ratingPrefix = ConfigurationManager.Configuration.MetadataCountryCode.Equals("us", StringComparison.OrdinalIgnoreCase) ? "" : ConfigurationManager.Configuration.MetadataCountryCode + "-";
                        movie.OfficialRating = !string.IsNullOrEmpty(ourRelease.certification)
                                                   ? ratingPrefix + ourRelease.certification
                                                   : !string.IsNullOrEmpty(usRelease.certification)
                                                         ? usRelease.certification
                                                         : !string.IsNullOrEmpty(minimunRelease.certification)
                                                               ? minimunRelease.iso_3166_1 + "-" + minimunRelease.certification
                                                               : null;
                    }

                    if (ourRelease.release_date != default(DateTime))
                    {
                        if (ourRelease.release_date.Year != 1)
                        {
                            movie.PremiereDate = ourRelease.release_date.ToUniversalTime();
                            movie.ProductionYear = ourRelease.release_date.Year;
                        }
                    }
                    else if (usRelease.release_date != default(DateTime))
                    {
                        if (usRelease.release_date.Year != 1)
                        {
                            movie.PremiereDate = usRelease.release_date.ToUniversalTime();
                            movie.ProductionYear = usRelease.release_date.Year;
                        }
                    }
                    else if (minimunRelease.release_date != default(DateTime))
                    {
                        if (minimunRelease.release_date.Year != 1)
                        {

                            movie.PremiereDate = minimunRelease.release_date.ToUniversalTime();
                            movie.ProductionYear = minimunRelease.release_date.Year;
                        }
                    }
                }
                else
                {
                    if (movieData.release_date.Year != 1)
                    {
                        //no specific country release info at all
                        movie.PremiereDate = movieData.release_date.ToUniversalTime();
                        movie.ProductionYear = movieData.release_date.Year;
                    }
                }

                //if that didn't find a rating and we are a boxset, use the one from our first child
                if (movie.OfficialRating == null && movie is BoxSet && !movie.LockedFields.Contains(MetadataFields.OfficialRating))
                {
                    var boxset = movie as BoxSet;
                    Logger.Info("MovieDbProvider - Using rating of first child of boxset...");

                    var firstChild = boxset.Children.Concat(boxset.GetLinkedChildren()).FirstOrDefault();

                    boxset.OfficialRating = firstChild != null ? firstChild.OfficialRating : null;
                }

                if (movieData.runtime > 0)
                    movie.OriginalRunTimeTicks = TimeSpan.FromMinutes(movieData.runtime).Ticks;

                //studios
                if (movieData.production_companies != null && !movie.LockedFields.Contains(MetadataFields.Studios))
                {
                    movie.Studios.Clear();

                    foreach (var studio in movieData.production_companies.Select(c => c.name))
                    {
                        movie.AddStudio(studio);
                    }
                }

                //genres
                if (movieData.genres != null && !movie.LockedFields.Contains(MetadataFields.Genres))
                {
                    movie.Genres.Clear();

                    foreach (var genre in movieData.genres.Select(g => g.name))
                    {
                        movie.AddGenre(genre);
                    }
                }

                if (!movie.LockedFields.Contains(MetadataFields.Cast))
                {
                    movie.People.Clear();

                    //Actors, Directors, Writers - all in People
                    //actors come from cast
                    if (movieData.casts != null && movieData.casts.cast != null)
                    {
                        foreach (var actor in movieData.casts.cast.OrderBy(a => a.order)) movie.AddPerson(new PersonInfo { Name = actor.name.Trim(), Role = actor.character, Type = PersonType.Actor });
                    }

                    //and the rest from crew
                    if (movieData.casts != null && movieData.casts.crew != null)
                    {
                        foreach (var person in movieData.casts.crew) movie.AddPerson(new PersonInfo { Name = person.name.Trim(), Role = person.job, Type = person.department });
                    }
                }

                if (movieData.keywords != null && movieData.keywords.keywords != null && !movie.LockedFields.Contains(MetadataFields.Tags))
                {
                    movie.Tags = movieData.keywords.keywords.Select(i => i.name).ToList();
                }

                if (movieData.trailers != null && movieData.trailers.youtube != null &&
                    movieData.trailers.youtube.Count > 0)
                {
                    movie.RemoteTrailers = movieData.trailers.youtube.Select(i => new MediaUrl
                    {
                        Url = string.Format("http://www.youtube.com/watch?v={0}", i.source),
                        IsDirectLink = false,
                        Name = i.name,
                        VideoSize = string.Equals("hd", i.size, StringComparison.OrdinalIgnoreCase) ? VideoSize.HighDefinition : VideoSize.StandardDefinition

                    }).ToList();
                }
            }

        }

        private DateTime _lastRequestDate = DateTime.MinValue;

        /// <summary>
        /// Gets the movie db response.
        /// </summary>
        internal async Task<Stream> GetMovieDbResponse(HttpRequestOptions options)
        {
            var cancellationToken = options.CancellationToken;

            await _movieDbResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Limit to three requests per second
                var diff = 340 - (DateTime.Now - _lastRequestDate).TotalMilliseconds;

                if (diff > 0)
                {
                    await Task.Delay(Convert.ToInt32(diff), cancellationToken).ConfigureAwait(false);
                }

                _lastRequestDate = DateTime.Now;

                return await HttpClient.Get(options).ConfigureAwait(false);
            }
            finally
            {
                _lastRequestDate = DateTime.Now;

                _movieDbResourcePool.Release();
            }
        }

        #region Result Objects


        /// <summary>
        /// Class TmdbTitle
        /// </summary>
        protected class TmdbTitle
        {
            /// <summary>
            /// Gets or sets the iso_3166_1.
            /// </summary>
            /// <value>The iso_3166_1.</value>
            public string iso_3166_1 { get; set; }
            /// <summary>
            /// Gets or sets the title.
            /// </summary>
            /// <value>The title.</value>
            public string title { get; set; }
        }

        /// <summary>
        /// Class TmdbAltTitleResults
        /// </summary>
        protected class TmdbAltTitleResults
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int id { get; set; }
            /// <summary>
            /// Gets or sets the titles.
            /// </summary>
            /// <value>The titles.</value>
            public List<TmdbTitle> titles { get; set; }
        }

        /// <summary>
        /// Class TmdbMovieSearchResult
        /// </summary>
        protected class TmdbMovieSearchResult
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
        protected class TmdbMovieSearchResults
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

        protected class BelongsToCollection
        {
            public int id { get; set; }
            public string name { get; set; }
            public string poster_path { get; set; }
            public string backdrop_path { get; set; }
        }

        protected class GenreItem
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        protected class ProductionCompany
        {
            public string name { get; set; }
            public int id { get; set; }
        }

        protected class ProductionCountry
        {
            public string iso_3166_1 { get; set; }
            public string name { get; set; }
        }

        protected class SpokenLanguage
        {
            public string iso_639_1 { get; set; }
            public string name { get; set; }
        }

        protected class Cast
        {
            public int id { get; set; }
            public string name { get; set; }
            public string character { get; set; }
            public int order { get; set; }
            public int cast_id { get; set; }
            public string profile_path { get; set; }
        }

        protected class Crew
        {
            public int id { get; set; }
            public string name { get; set; }
            public string department { get; set; }
            public string job { get; set; }
            public string profile_path { get; set; }
        }

        protected class Casts
        {
            public List<Cast> cast { get; set; }
            public List<Crew> crew { get; set; }
        }

        protected class Country
        {
            public string iso_3166_1 { get; set; }
            public string certification { get; set; }
            public DateTime release_date { get; set; }
        }

        protected class Releases
        {
            public List<Country> countries { get; set; }
        }

        protected class Keyword
        {
            public int id { get; set; }
            public string name { get; set; }
        }

        protected class Keywords
        {
            public List<Keyword> keywords { get; set; }
        }

        protected class CompleteMovieData
        {
            public bool adult { get; set; }
            public string backdrop_path { get; set; }
            public BelongsToCollection belongs_to_collection { get; set; }
            public int budget { get; set; }
            public List<GenreItem> genres { get; set; }
            public string homepage { get; set; }
            public int id { get; set; }
            public string imdb_id { get; set; }
            public string name { get; set; }
            public string original_title { get; set; }
            public string overview { get; set; }
            public double popularity { get; set; }
            public string poster_path { get; set; }
            public List<ProductionCompany> production_companies { get; set; }
            public List<ProductionCountry> production_countries { get; set; }
            public DateTime release_date { get; set; }
            public int revenue { get; set; }
            public int runtime { get; set; }
            public List<SpokenLanguage> spoken_languages { get; set; }
            public string status { get; set; }
            public string tagline { get; set; }
            public string title { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
            public Casts casts { get; set; }
            public Releases releases { get; set; }
            public Keywords keywords { get; set; }
            public Trailers trailers { get; set; }
        }

        public class Trailers
        {
            public List<Youtube> youtube { get; set; }
        }

        public class Youtube
        {
            public string name { get; set; }
            public string size { get; set; }
            public string source { get; set; }
        }

        public class TmdbImageSettings
        {
            public List<string> backdrop_sizes { get; set; }
            public string base_url { get; set; }
            public List<string> poster_sizes { get; set; }
            public List<string> profile_sizes { get; set; }
        }

        public class TmdbSettingsResult
        {
            public TmdbImageSettings images { get; set; }
        }
        #endregion

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
