using System.Net;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Providers.Movies
{
    class MovieDbProviderException : ApplicationException
    {
        public MovieDbProviderException(string msg) : base(msg)
        {
        }
     
    }
    /// <summary>
    /// Class MovieDbProvider
    /// </summary>
    public class MovieDbProvider : BaseMetadataProvider
    {
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
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer</exception>
        public MovieDbProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient)
            : base()
        {
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            JsonSerializer = jsonSerializer;
            HttpClient = httpClient;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.Second; }
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Movie || item is BoxSet;
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
        /// If we save locally, refresh if they delete something
        /// </summary>
        protected override bool RefreshOnFileSystemStampChange
        {
            get
            {
                return Kernel.Instance.Configuration.SaveLocalMeta;
            }
        }

        /// <summary>
        /// The _TMDB settings task
        /// </summary>
        private Task<TmdbSettingsResult> _tmdbSettingsTask;
        /// <summary>
        /// The _TMDB settings task initialized
        /// </summary>
        private bool _tmdbSettingsTaskInitialized;
        /// <summary>
        /// The _TMDB settings task sync lock
        /// </summary>
        private object _tmdbSettingsTaskSyncLock = new object();

        /// <summary>
        /// Gets the TMDB settings.
        /// </summary>
        /// <value>The TMDB settings.</value>
        public Task<TmdbSettingsResult> TmdbSettings
        {
            get
            {
                LazyInitializer.EnsureInitialized(ref _tmdbSettingsTask, ref _tmdbSettingsTaskInitialized, ref _tmdbSettingsTaskSyncLock, () => GetTmdbSettings(JsonSerializer, HttpClient));
                return _tmdbSettingsTask;
            }
        }

        /// <summary>
        /// Gets the TMDB settings.
        /// </summary>
        /// <returns>Task{TmdbSettingsResult}.</returns>
        private static async Task<TmdbSettingsResult> GetTmdbSettings(IJsonSerializer jsonSerializer, IHttpClient httpClient)
        {
            try
            {
                using (var json = await httpClient.Get(String.Format(TmdbConfigUrl, ApiKey), Kernel.Instance.ResourcePools.MovieDb, CancellationToken.None).ConfigureAwait(false))
                {
                    return jsonSerializer.DeserializeFromStream<TmdbSettingsResult>(json);
                }
            }
            catch (HttpException)
            {
                return new TmdbSettingsResult
                {
                    images = new TmdbImageSettings
                    {
                        backdrop_sizes =
                            new List<string>
                                                                                                     {
                                                                                                         "w380",
                                                                                                         "w780",
                                                                                                         "w1280",
                                                                                                         "original"
                                                                                                     },
                        poster_sizes =
                            new List<string>
                                                                                                     {
                                                                                                         "w92",
                                                                                                         "w154",
                                                                                                         "w185",
                                                                                                         "w342",
                                                                                                         "w500",
                                                                                                         "original"
                                                                                                     },
                        profile_sizes =
                            new List<string>
                                                                                                     {
                                                                                                         "w45",
                                                                                                         "w185",
                                                                                                         "h632",
                                                                                                         "original"
                                                                                                     },
                        base_url = "http://cf2.imgobject.com/t/p/"

                    }
                }; 
            }
        }

        /// <summary>
        /// The json provider
        /// </summary>
        protected MovieProviderFromJson JsonProvider;
        /// <summary>
        /// Sets the persisted last refresh date on the item for this provider.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="value">The value.</param>
        /// <param name="status">The status.</param>
        protected override void SetLastRefreshed(BaseItem item, DateTime value, ProviderRefreshStatus status = ProviderRefreshStatus.Success)
        {
            base.SetLastRefreshed(item, value, status);

            if (Kernel.Instance.Configuration.SaveLocalMeta)
            {
                //in addition to ours, we need to set the last refreshed time for the local data provider
                //so it won't see the new files we download and process them all over again
                if (JsonProvider == null) JsonProvider = new MovieProviderFromJson(HttpClient, JsonSerializer);
                var data = item.ProviderData.GetValueOrDefault(JsonProvider.Id, new BaseProviderInfo { ProviderId = JsonProvider.Id });
                data.LastRefreshed = value;
                item.ProviderData[JsonProvider.Id] = data;
            }
        }

        private const string TmdbConfigUrl = "http://api.themoviedb.org/3/configuration?api_key={0}";
        private const string Search3 = @"http://api.themoviedb.org/3/search/movie?api_key={1}&query={0}&language={2}";
        private const string AltTitleSearch = @"http://api.themoviedb.org/3/movie/{0}/alternative_titles?api_key={1}&country={2}";
        private const string GetInfo3 = @"http://api.themoviedb.org/3/{3}/{0}?api_key={1}&language={2}";
        private const string CastInfo = @"http://api.themoviedb.org/3/movie/{0}/casts?api_key={1}";
        private const string ReleaseInfo = @"http://api.themoviedb.org/3/movie/{0}/releases?api_key={1}";
        private const string GetImages = @"http://api.themoviedb.org/3/{2}/{0}/images?api_key={1}";
        public static string ApiKey = "f6bd687ffa63cd282b6ff2c6877f2669";

        static readonly Regex[] NameMatches = new[] {
            new Regex(@"(?<name>.*)\((?<year>\d{4})\)"), // matches "My Movie (2001)" and gives us the name and the year
            new Regex(@"(?<name>.*)") // last resort matches the whole string as the name
        };

        public const string LOCAL_META_FILE_NAME = "MBMovie.json";
        public const string ALT_META_FILE_NAME = "movie.xml";
        protected string ItemType = "movie";

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (item.DontFetchMeta) return false;

            if (Kernel.Instance.Configuration.SaveLocalMeta && HasFileSystemStampChanged(item, providerInfo))
            {
                //If they deleted something from file system, chances are, this item was mis-identified the first time
                item.SetProviderId(MetadataProviders.Tmdb, null);
                Logger.Debug("MovieProvider reports file system stamp change...");
                return true;

            }

            if (providerInfo.LastRefreshStatus == ProviderRefreshStatus.CompletedWithErrors)
            {
                Logger.Debug("MovieProvider for {0} - last attempt had errors.  Will try again.", item.Path);
                return true;
            }

            var downloadDate = providerInfo.LastRefreshed;

            if (Kernel.Instance.Configuration.MetadataRefreshDays == -1 && downloadDate != DateTime.MinValue)
            {
                return false;
            }

            if (DateTime.Today.Subtract(item.DateCreated).TotalDays > 180 && downloadDate != DateTime.MinValue)
                return false; // don't trigger a refresh data for item that are more than 6 months old and have been refreshed before

            if (DateTime.Today.Subtract(downloadDate).TotalDays < Kernel.Instance.Configuration.MetadataRefreshDays) // only refresh every n days
                return false;

            if (HasAltMeta(item))
                return false; //never refresh if has meta from other source



            Logger.Debug("MovieDbProvider - " + item.Name + " needs refresh.  Download date: " + downloadDate + " item created date: " + item.DateCreated + " Check for Update age: " + Kernel.Instance.Configuration.MetadataRefreshDays);
            return true;
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <returns>Task{System.Boolean}.</returns>
        protected override async Task<bool> FetchAsyncInternal(BaseItem item, bool force, CancellationToken cancellationToken)
        {
            if (HasAltMeta(item))
            {
                Logger.Info("MovieDbProvider - Not fetching because 3rd party meta exists for " + item.Name);
                SetLastRefreshed(item, DateTime.UtcNow);
                return true;
            }
            if (item.DontFetchMeta)
            {
                Logger.Info("MovieDbProvider - Not fetching because requested to ignore " + item.Name);
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!Kernel.Instance.Configuration.SaveLocalMeta || !HasLocalMeta(item) || (force && !HasLocalMeta(item)))
            {
                try
                {
                    await FetchMovieData(item, cancellationToken).ConfigureAwait(false);
                    SetLastRefreshed(item, DateTime.UtcNow);
                }
                catch (MovieDbProviderException)
                {
                    SetLastRefreshed(item, DateTime.UtcNow, ProviderRefreshStatus.CompletedWithErrors);
                }

                return true;
            }
            Logger.Debug("MovieDBProvider not fetching because local meta exists for " + item.Name);
            SetLastRefreshed(item, DateTime.UtcNow);
            return true;
        }

        /// <summary>
        /// Determines whether [has local meta] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [has local meta] [the specified item]; otherwise, <c>false</c>.</returns>
        private bool HasLocalMeta(BaseItem item)
        {
            //need at least the xml and folder.jpg/png or a movie.xml put in by someone else
            return item.ResolveArgs.ContainsMetaFileByName(LOCAL_META_FILE_NAME);
        }

        /// <summary>
        /// Determines whether [has alt meta] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if [has alt meta] [the specified item]; otherwise, <c>false</c>.</returns>
        private bool HasAltMeta(BaseItem item)
        {
            return item.ResolveArgs.ContainsMetaFileByName(ALT_META_FILE_NAME);
        }

        /// <summary>
        /// Fetches the movie data.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Task.</returns>
        private async Task FetchMovieData(BaseItem item, CancellationToken cancellationToken)
        {
            string id = item.GetProviderId(MetadataProviders.Tmdb) ?? await FindId(item, item.ProductionYear, cancellationToken).ConfigureAwait(false);
            if (id != null)
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
        /// <param name="productionYear">The production year.</param>
        /// <returns>Task{System.String}.</returns>
        public async Task<string> FindId(BaseItem item, int? productionYear, CancellationToken cancellationToken)
        {
            string justName = item.Path != null ? item.Path.Substring(item.Path.LastIndexOf(Path.DirectorySeparatorChar)) : string.Empty;
            var id = justName.GetAttributeValue("tmdbid");
            if (id != null)
            {
                Logger.Debug("Using tmdb id specified in path.");
                return id;
            }

            int? year;
            string name = item.Name;
            ParseName(name, out name, out year);

            if (year == null && productionYear != null)
            {
                year = productionYear;
            }

            Logger.Info("MovieDbProvider: Finding id for movie: " + name);
            string language = Kernel.Instance.Configuration.PreferredMetadataLanguage.ToLower();

            //if we are a boxset - look at our first child
            var boxset = item as BoxSet;
            if (boxset != null)
            {
                if (!boxset.Children.IsEmpty)
                {
                    var firstChild = boxset.Children.First();
                    Logger.Debug("MovieDbProvider - Attempting to find boxset ID from: " + firstChild.Name);
                    string childName;
                    int? childYear;
                    ParseName(firstChild.Name, out childName, out childYear);
                    id = await GetBoxsetIdFromMovie(childName, childYear, language, cancellationToken).ConfigureAwait(false);
                    if (id != null)
                    {
                        Logger.Info("MovieDbProvider - Found Boxset ID: " + id);
                    }
                }

                return id;
            }
            //nope - search for it
            id = await AttemptFindId(name, year, language, cancellationToken).ConfigureAwait(false);
            if (id == null)
            {
                //try in english if wasn't before
                if (language != "en")
                {
                    id = await AttemptFindId(name, year, "en", cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // try with dot and _ turned to space
                    name = name.Replace(",", " ");
                    name = name.Replace(".", " ");
                    name = name.Replace("  ", " ");
                    name = name.Replace("_", " ");
                    name = name.Replace("-", "");
                    id = await AttemptFindId(name, year, language, cancellationToken).ConfigureAwait(false);
                    if (id == null && language != "en")
                    {
                        //one more time, in english
                        id = await AttemptFindId(name, year, "en", cancellationToken).ConfigureAwait(false);

                    }
                    if (id == null)
                    {
                        //last resort - try using the actual folder name
                        id = await AttemptFindId(Path.GetFileName(item.ResolveArgs.Path), year, "en", cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            return id;
        }

        /// <summary>
        /// Attempts the find id.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="year">The year.</param>
        /// <param name="language">The language.</param>
        /// <returns>Task{System.String}.</returns>
        public virtual async Task<string> AttemptFindId(string name, int? year, string language, CancellationToken cancellationToken)
        {
            string url3 = string.Format(Search3, UrlEncode(name), ApiKey, language);
            TmdbMovieSearchResults searchResult = null;

            try
            {
                using (Stream json = await HttpClient.Get(url3, Kernel.Instance.ResourcePools.MovieDb, cancellationToken).ConfigureAwait(false))
                {
                    searchResult = JsonSerializer.DeserializeFromStream<TmdbMovieSearchResults>(json);
                }
            }
            catch (HttpException)
            {
            }
            if (searchResult == null || searchResult.results.Count == 0)
            {
                //try replacing numbers
                foreach (var pair in ReplaceStartNumbers)
                {
                    if (name.StartsWith(pair.Key))
                    {
                        name = name.Remove(0, pair.Key.Length);
                        name = pair.Value + name;
                    }
                }
                foreach (var pair in ReplaceEndNumbers)
                {
                    if (name.EndsWith(pair.Key))
                    {
                        name = name.Remove(name.IndexOf(pair.Key), pair.Key.Length);
                        name = name + pair.Value;
                    }
                }
                Logger.Info("MovieDBProvider - No results.  Trying replacement numbers: " + name);
                url3 = string.Format(Search3, UrlEncode(name), ApiKey, language);

                try
                {
                    using (Stream json = await HttpClient.Get(url3, Kernel.Instance.ResourcePools.MovieDb, cancellationToken).ConfigureAwait(false))
                    {
                        searchResult = JsonSerializer.DeserializeFromStream<TmdbMovieSearchResults>(json);
                    }
                }
                catch (HttpException)
                {
                }
            }
            if (searchResult != null)
            {
                string compName = GetComparableName(name, Logger);
                foreach (var possible in searchResult.results)
                {
                    string matchedName = null;
                    string id = possible.id.ToString();
                    string n = possible.title;
                    if (GetComparableName(n, Logger) == compName)
                    {
                        matchedName = n;
                    }
                    else
                    {
                        n = possible.original_title;
                        if (GetComparableName(n, Logger) == compName)
                        {
                            matchedName = n;
                        }
                    }

                    Logger.Debug("MovieDbProvider - " + compName + " didn't match " + n);
                    //if main title matches we don't have to look for alternatives
                    if (matchedName == null)
                    {
                        //that title didn't match - look for alternatives
                        url3 = string.Format(AltTitleSearch, id, ApiKey, Kernel.Instance.Configuration.MetadataCountryCode);

                        try
                        {
                            using (Stream json = await HttpClient.Get(url3, Kernel.Instance.ResourcePools.MovieDb, cancellationToken).ConfigureAwait(false))
                            {
                                var response = JsonSerializer.DeserializeFromStream<TmdbAltTitleResults>(json);

                                if (response != null && response.titles != null)
                                {
                                    foreach (var title in response.titles)
                                    {
                                        var t = GetComparableName(title.title, Logger);
                                        if (t == compName)
                                        {
                                            Logger.Debug("MovieDbProvider - " + compName +
                                                                " matched " + t);
                                            matchedName = t;
                                            break;
                                        }
                                        Logger.Debug("MovieDbProvider - " + compName +
                                                            " did not match " + t);
                                    }
                                }
                            }
                        }
                        catch (HttpException)
                        {
                        }
                    }

                    if (matchedName != null)
                    {
                        Logger.Debug("Match " + matchedName + " for " + name);
                        if (year != null)
                        {
                            DateTime r;

                            if (DateTime.TryParse(possible.release_date, out r))
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
        /// Gets the boxset id from movie.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="year">The year.</param>
        /// <param name="language">The language.</param>
        /// <returns>Task{System.String}.</returns>
        protected async Task<string> GetBoxsetIdFromMovie(string name, int? year, string language, CancellationToken cancellationToken)
        {
            string id = null;
            string childId = await AttemptFindId(name, year, language, cancellationToken).ConfigureAwait(false);
            if (childId != null)
            {
                string url = string.Format(GetInfo3, childId, ApiKey, language, ItemType);

                try
                {
                    using (Stream json = await HttpClient.Get(url, Kernel.Instance.ResourcePools.MovieDb, cancellationToken).ConfigureAwait(false))
                    {
                        var movieResult = JsonSerializer.DeserializeFromStream<CompleteMovieData>(json);

                        if (movieResult != null && movieResult.belongs_to_collection != null)
                        {
                            id = movieResult.belongs_to_collection.id.ToString();
                        }
                        else
                        {
                            Logger.Error("Unable to obtain boxset id.");
                        }
                    }
                }
                catch (HttpException)
                {
                }
            }
            return id;
        }

        /// <summary>
        /// Fetches the movie data.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="id">The id.</param>
        /// <returns>Task.</returns>
        protected async Task FetchMovieData(BaseItem item, string id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (String.IsNullOrEmpty(id))
            {
                Logger.Info("MoviedbProvider: Ignoring " + item.Name + " because ID forced blank.");
                return;
            }
            if (item.GetProviderId(MetadataProviders.Tmdb) == null) item.SetProviderId(MetadataProviders.Tmdb, id);
            var mainTask = FetchMainResult(item, id, cancellationToken);
            var castTask = FetchCastInfo(item, id, cancellationToken);
            var releaseTask = FetchReleaseInfo(item, id, cancellationToken);
            var imageTask = FetchImageInfo(item, id, cancellationToken);

            await Task.WhenAll(mainTask, castTask, releaseTask).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var mainResult = mainTask.Result;
            if (mainResult == null) return;

            if (castTask.Result != null)
            {
                mainResult.cast = castTask.Result.cast;
                mainResult.crew = castTask.Result.crew;
            }

            if (releaseTask.Result != null)
            {
                mainResult.countries = releaseTask.Result.countries;
            }

            ProcessMainInfo(item, mainResult);

            await Task.WhenAll(imageTask).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (imageTask.Result != null)
            {
                await ProcessImages(item, imageTask.Result, cancellationToken).ConfigureAwait(false);
            }

            //and save locally
            if (Kernel.Instance.Configuration.SaveLocalMeta)
            {
                var ms = new MemoryStream();
                JsonSerializer.SerializeToStream(mainResult, ms);

                cancellationToken.ThrowIfCancellationRequested();

                await Kernel.Instance.FileSystemManager.SaveToLibraryFilesystem(item, Path.Combine(item.MetaLocation, LOCAL_META_FILE_NAME), ms, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Fetches the main result.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="id">The id.</param>
        /// <returns>Task{CompleteMovieData}.</returns>
        protected async Task<CompleteMovieData> FetchMainResult(BaseItem item, string id, CancellationToken cancellationToken)
        {
            ItemType = item is BoxSet ? "collection" : "movie";
            string url = string.Format(GetInfo3, id, ApiKey, Kernel.Instance.Configuration.PreferredMetadataLanguage, ItemType);
            CompleteMovieData mainResult = null;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using (var json = await HttpClient.Get(url, Kernel.Instance.ResourcePools.MovieDb, cancellationToken).ConfigureAwait(false))
                {
                    mainResult = JsonSerializer.DeserializeFromStream<CompleteMovieData>(json);
                }
            }
            catch (HttpException e)
            {
                if (e.IsTimedOut)
                {
                    Logger.ErrorException("MovieDbProvider timed out attempting to retrieve main info for {0}", e, item.Path);
                    throw new MovieDbProviderException("Timed out on main info");
                }
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    Logger.ErrorException("MovieDbProvider not found error attempting to retrieve main info for {0}", e, item.Path);
                    throw new MovieDbProviderException("Not Found");
                }

                throw;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (mainResult != null && string.IsNullOrEmpty(mainResult.overview))
            {
                if (Kernel.Instance.Configuration.PreferredMetadataLanguage.ToLower() != "en")
                {
                    Logger.Info("MovieDbProvider couldn't find meta for language " + Kernel.Instance.Configuration.PreferredMetadataLanguage + ". Trying English...");
                    url = string.Format(GetInfo3, id, ApiKey, "en", ItemType);

                    try
                    {
                        using (Stream json = await HttpClient.Get(url, Kernel.Instance.ResourcePools.MovieDb, cancellationToken).ConfigureAwait(false))
                        {
                            mainResult = JsonSerializer.DeserializeFromStream<CompleteMovieData>(json);
                        }
                    }
                    catch (HttpException)
                    {
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
        /// Fetches the cast info.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="id">The id.</param>
        /// <returns>Task{TmdbCastResult}.</returns>
        protected async Task<TmdbCastResult> FetchCastInfo(BaseItem item, string id, CancellationToken cancellationToken)
        {
            //get cast and crew info
            var url = string.Format(CastInfo, id, ApiKey, ItemType);
            TmdbCastResult cast = null;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using (Stream json = await HttpClient.Get(url, Kernel.Instance.ResourcePools.MovieDb, cancellationToken).ConfigureAwait(false))
                {
                    cast = JsonSerializer.DeserializeFromStream<TmdbCastResult>(json);
                }
            }
            catch (HttpException)
            {
            }
            return cast;
        }

        /// <summary>
        /// Fetches the release info.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="id">The id.</param>
        /// <returns>Task{TmdbReleasesResult}.</returns>
        protected async Task<TmdbReleasesResult> FetchReleaseInfo(BaseItem item, string id, CancellationToken cancellationToken)
        {
            var url = string.Format(ReleaseInfo, id, ApiKey);
            TmdbReleasesResult releases = null;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using (Stream json = await HttpClient.Get(url, Kernel.Instance.ResourcePools.MovieDb, cancellationToken).ConfigureAwait(false))
                {
                    releases = JsonSerializer.DeserializeFromStream<TmdbReleasesResult>(json);
                }
            }
            catch (HttpException)
            {
            }

            return releases;
        }

        /// <summary>
        /// Fetches the image info.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="id">The id.</param>
        /// <returns>Task{TmdbImages}.</returns>
        protected async Task<TmdbImages> FetchImageInfo(BaseItem item, string id, CancellationToken cancellationToken)
        {
            //fetch images
            var url = string.Format(GetImages, id, ApiKey, ItemType);
            TmdbImages images = null;

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using (Stream json = await HttpClient.Get(url, Kernel.Instance.ResourcePools.MovieDb, cancellationToken).ConfigureAwait(false))
                {
                    images = JsonSerializer.DeserializeFromStream<TmdbImages>(json);
                }
            }
            catch (HttpException)
            {
            }
            return images;
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

                movie.Name = movieData.title ?? movieData.original_title ?? movie.Name;
                movie.Overview = movieData.overview;
                movie.Overview = movie.Overview != null ? movie.Overview.Replace("\n\n", "\n") : null;
                if (!string.IsNullOrEmpty(movieData.tagline)) movie.AddTagline(movieData.tagline);
                movie.SetProviderId(MetadataProviders.Imdb, movieData.imdb_id);
                float rating;
                string voteAvg = movieData.vote_average.ToString();
                string cultureStr = Kernel.Instance.Configuration.PreferredMetadataLanguage + "-" + Kernel.Instance.Configuration.MetadataCountryCode;
                CultureInfo culture;
                try
                {
                    culture = new CultureInfo(cultureStr);
                }
                catch
                {
                    culture = CultureInfo.CurrentCulture; //default to windows settings if other was invalid
                }
                Logger.Debug("Culture for numeric conversion is: " + culture.Name);
                if (float.TryParse(voteAvg, NumberStyles.AllowDecimalPoint, culture, out rating))
                    movie.CommunityRating = rating;

                //release date and certification are retrieved based on configured country and we fall back on US if not there
                if (movieData.countries != null)
                {
                    var ourRelease = movieData.countries.FirstOrDefault(c => c.iso_3166_1.Equals(Kernel.Instance.Configuration.MetadataCountryCode, StringComparison.OrdinalIgnoreCase)) ?? new Country();
                    var usRelease = movieData.countries.FirstOrDefault(c => c.iso_3166_1.Equals("US", StringComparison.OrdinalIgnoreCase)) ?? new Country();

                    movie.OfficialRating = ourRelease.certification ?? usRelease.certification;
                    if (ourRelease.release_date > new DateTime(1900, 1, 1))
                    {
                        movie.PremiereDate = ourRelease.release_date;
                        movie.ProductionYear = ourRelease.release_date.Year;
                    }
                    else
                    {
                        movie.PremiereDate = usRelease.release_date;
                        movie.ProductionYear = usRelease.release_date.Year;
                    }
                }
                else
                {
                    //no specific country release info at all
                    movie.PremiereDate = movieData.release_date;
                    movie.ProductionYear = movieData.release_date.Year;
                }

                //if that didn't find a rating and we are a boxset, use the one from our first child
                if (movie.OfficialRating == null && movie is BoxSet)
                {
                    var boxset = movie as BoxSet;
                    Logger.Info("MovieDbProvider - Using rating of first child of boxset...");
                    boxset.OfficialRating = !boxset.Children.IsEmpty ? boxset.Children.First().OfficialRating : null;
                }

                if (movie.RunTimeTicks == null && movieData.runtime > 0)
                    movie.RunTimeTicks = TimeSpan.FromMinutes(movieData.runtime).Ticks;

                //studios
                if (movieData.production_companies != null)
                {
                    //always clear so they don't double up
                    movie.AddStudios(movieData.production_companies.Select(c => c.name));
                }

                //genres
                if (movieData.genres != null)
                {
                    movie.AddGenres(movieData.genres.Select(g => g.name));
                }

                //Actors, Directors, Writers - all in People
                //actors come from cast
                if (movieData.cast != null)
                {
                    foreach (var actor in movieData.cast.OrderBy(a => a.order)) movie.AddPerson(new PersonInfo { Name = actor.name, Role = actor.character, Type = PersonType.Actor });
                }
                //and the rest from crew
                if (movieData.crew != null)
                {
                    foreach (var person in movieData.crew) movie.AddPerson(new PersonInfo { Name = person.name, Role = person.job, Type = person.department });
                }


            }

        }

        /// <summary>
        /// Processes the images.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="images">The images.</param>
        /// <returns>Task.</returns>
        protected virtual async Task ProcessImages(BaseItem item, TmdbImages images, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            //        poster
            if (images.posters != null && images.posters.Count > 0 && (Kernel.Instance.Configuration.RefreshItemImages || !item.HasLocalImage("folder")))
            {
                var tmdbSettings = await TmdbSettings.ConfigureAwait(false);

                var tmdbImageUrl = tmdbSettings.images.base_url + Kernel.Instance.Configuration.TmdbFetchedPosterSize;
                // get highest rated poster for our language

                var postersSortedByVote = images.posters.OrderByDescending(i => i.vote_average);

                var poster = postersSortedByVote.FirstOrDefault(p => p.iso_639_1 != null && p.iso_639_1.Equals(Kernel.Instance.Configuration.PreferredMetadataLanguage, StringComparison.OrdinalIgnoreCase));
                if (poster == null && !Kernel.Instance.Configuration.PreferredMetadataLanguage.Equals("en"))
                {
                    // couldn't find our specific language, find english (if that wasn't our language)
                    poster = postersSortedByVote.FirstOrDefault(p => p.iso_639_1 != null && p.iso_639_1.Equals("en", StringComparison.OrdinalIgnoreCase));
                }
                if (poster == null)
                {
                    //still couldn't find it - try highest rated null one
                    poster = postersSortedByVote.FirstOrDefault(p => p.iso_639_1 == null);
                }
                if (poster == null)
                {
                    //finally - just get the highest rated one
                    poster = postersSortedByVote.FirstOrDefault();
                }
                if (poster != null)
                {
                    try
                    {
                        item.PrimaryImagePath = await Kernel.Instance.ProviderManager.DownloadAndSaveImage(item, tmdbImageUrl + poster.file_path, "folder" + Path.GetExtension(poster.file_path), Kernel.Instance.ResourcePools.MovieDb, cancellationToken).ConfigureAwait(false);
                    }
                    catch (HttpException)
                    {
                    }
                    catch (IOException)
                    {

                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // backdrops
            if (images.backdrops != null && images.backdrops.Count > 0)
            {
                item.BackdropImagePaths = new List<string>();

                var tmdbSettings = await TmdbSettings.ConfigureAwait(false);

                var tmdbImageUrl = tmdbSettings.images.base_url + Kernel.Instance.Configuration.TmdbFetchedBackdropSize;
                //backdrops should be in order of rating.  get first n ones
                var numToFetch = Math.Min(Kernel.Instance.Configuration.MaxBackdrops, images.backdrops.Count);
                for (var i = 0; i < numToFetch; i++)
                {
                    var bdName = "backdrop" + (i == 0 ? "" : i.ToString());

                    if (Kernel.Instance.Configuration.RefreshItemImages || !item.HasLocalImage(bdName))
                    {
                        try
                        {
                            item.BackdropImagePaths.Add(await Kernel.Instance.ProviderManager.DownloadAndSaveImage(item, tmdbImageUrl + images.backdrops[i].file_path, bdName + Path.GetExtension(images.backdrops[i].file_path), Kernel.Instance.ResourcePools.MovieDb, cancellationToken).ConfigureAwait(false));
                        }
                        catch (HttpException)
                        {
                        }
                        catch (IOException)
                        {

                        }
                    }
                }
            }
        }


        /// <summary>
        /// The remove
        /// </summary>
        const string remove = "\"'!`?";
        // "Face/Off" support.
        /// <summary>
        /// The spacers
        /// </summary>
        const string spacers = "/,.:;\\(){}[]+-_=–*";  // (there are not actually two - in the they are different char codes)
        /// <summary>
        /// The replace start numbers
        /// </summary>
        static readonly Dictionary<string, string> ReplaceStartNumbers = new Dictionary<string, string> {
            {"1 ","one "},
            {"2 ","two "},
            {"3 ","three "},
            {"4 ","four "},
            {"5 ","five "},
            {"6 ","six "},
            {"7 ","seven "},
            {"8 ","eight "},
            {"9 ","nine "},
            {"10 ","ten "},
            {"11 ","eleven "},
            {"12 ","twelve "},
            {"13 ","thirteen "},
            {"100 ","one hundred "},
            {"101 ","one hundred one "}
        };

        /// <summary>
        /// The replace end numbers
        /// </summary>
        static readonly Dictionary<string, string> ReplaceEndNumbers = new Dictionary<string, string> {
            {" 1"," i"},
            {" 2"," ii"},
            {" 3"," iii"},
            {" 4"," iv"},
            {" 5"," v"},
            {" 6"," vi"},
            {" 7"," vii"},
            {" 8"," viii"},
            {" 9"," ix"},
            {" 10"," x"}
        };

        /// <summary>
        /// Gets the name of the comparable.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="logger">The logger.</param>
        /// <returns>System.String.</returns>
        internal static string GetComparableName(string name, ILogger logger)
        {
            name = name.ToLower();
            name = name.Replace("á", "a");
            name = name.Replace("é", "e");
            name = name.Replace("í", "i");
            name = name.Replace("ó", "o");
            name = name.Replace("ú", "u");
            name = name.Replace("ü", "u");
            name = name.Replace("ñ", "n");
            foreach (var pair in ReplaceStartNumbers)
            {
                if (name.StartsWith(pair.Key))
                {
                    name = name.Remove(0, pair.Key.Length);
                    name = pair.Value + name;
                    logger.Info("MovieDbProvider - Replaced Start Numbers: " + name);
                }
            }
            foreach (var pair in ReplaceEndNumbers)
            {
                if (name.EndsWith(pair.Key))
                {
                    name = name.Remove(name.IndexOf(pair.Key), pair.Key.Length);
                    name = name + pair.Value;
                    logger.Info("MovieDbProvider - Replaced End Numbers: " + name);
                }
            }
            name = name.Normalize(NormalizationForm.FormKD);
            var sb = new StringBuilder();
            foreach (var c in name)
            {
                if ((int)c >= 0x2B0 && (int)c <= 0x0333)
                {
                    // skip char modifier and diacritics 
                }
                else if (remove.IndexOf(c) > -1)
                {
                    // skip chars we are removing
                }
                else if (spacers.IndexOf(c) > -1)
                {
                    sb.Append(" ");
                }
                else if (c == '&')
                {
                    sb.Append(" and ");
                }
                else
                {
                    sb.Append(c);
                }
            }
            name = sb.ToString();
            name = name.Replace(", the", "");
            name = name.Replace(" the ", " ");
            name = name.Replace("the ", "");

            string prev_name;
            do
            {
                prev_name = name;
                name = name.Replace("  ", " ");
            } while (name.Length != prev_name.Length);

            return name.Trim();
        }

        #region Result Objects


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

        /// <summary>
        /// Class BelongsToCollection
        /// </summary>
        protected class BelongsToCollection
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int id { get; set; }
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string name { get; set; }
            /// <summary>
            /// Gets or sets the poster_path.
            /// </summary>
            /// <value>The poster_path.</value>
            public string poster_path { get; set; }
            /// <summary>
            /// Gets or sets the backdrop_path.
            /// </summary>
            /// <value>The backdrop_path.</value>
            public string backdrop_path { get; set; }
        }

        /// <summary>
        /// Class Genre
        /// </summary>
        protected class Genre
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int id { get; set; }
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string name { get; set; }
        }

        /// <summary>
        /// Class ProductionCompany
        /// </summary>
        protected class ProductionCompany
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string name { get; set; }
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int id { get; set; }
        }

        /// <summary>
        /// Class ProductionCountry
        /// </summary>
        protected class ProductionCountry
        {
            /// <summary>
            /// Gets or sets the iso_3166_1.
            /// </summary>
            /// <value>The iso_3166_1.</value>
            public string iso_3166_1 { get; set; }
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string name { get; set; }
        }

        /// <summary>
        /// Class SpokenLanguage
        /// </summary>
        protected class SpokenLanguage
        {
            /// <summary>
            /// Gets or sets the iso_639_1.
            /// </summary>
            /// <value>The iso_639_1.</value>
            public string iso_639_1 { get; set; }
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string name { get; set; }
        }

        /// <summary>
        /// Class Cast
        /// </summary>
        protected class Cast
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int id { get; set; }
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string name { get; set; }
            /// <summary>
            /// Gets or sets the character.
            /// </summary>
            /// <value>The character.</value>
            public string character { get; set; }
            /// <summary>
            /// Gets or sets the order.
            /// </summary>
            /// <value>The order.</value>
            public int order { get; set; }
            /// <summary>
            /// Gets or sets the profile_path.
            /// </summary>
            /// <value>The profile_path.</value>
            public string profile_path { get; set; }
        }

        /// <summary>
        /// Class Crew
        /// </summary>
        protected class Crew
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int id { get; set; }
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string name { get; set; }
            /// <summary>
            /// Gets or sets the department.
            /// </summary>
            /// <value>The department.</value>
            public string department { get; set; }
            /// <summary>
            /// Gets or sets the job.
            /// </summary>
            /// <value>The job.</value>
            public string job { get; set; }
            /// <summary>
            /// Gets or sets the profile_path.
            /// </summary>
            /// <value>The profile_path.</value>
            public object profile_path { get; set; }
        }

        /// <summary>
        /// Class Country
        /// </summary>
        protected class Country
        {
            /// <summary>
            /// Gets or sets the iso_3166_1.
            /// </summary>
            /// <value>The iso_3166_1.</value>
            public string iso_3166_1 { get; set; }
            /// <summary>
            /// Gets or sets the certification.
            /// </summary>
            /// <value>The certification.</value>
            public string certification { get; set; }
            /// <summary>
            /// Gets or sets the release_date.
            /// </summary>
            /// <value>The release_date.</value>
            public DateTime release_date { get; set; }
        }

        //protected class TmdbMovieResult
        //{
        //    public bool adult { get; set; }
        //    public string backdrop_path { get; set; }
        //    public int belongs_to_collection { get; set; }
        //    public int budget { get; set; }
        //    public List<Genre> genres { get; set; }
        //    public string homepage { get; set; }
        //    public int id { get; set; }
        //    public string imdb_id { get; set; }
        //    public string original_title { get; set; }
        //    public string overview { get; set; }
        //    public double popularity { get; set; }
        //    public string poster_path { get; set; }
        //    public List<ProductionCompany> production_companies { get; set; }
        //    public List<ProductionCountry> production_countries { get; set; }
        //    public string release_date { get; set; }
        //    public int revenue { get; set; }
        //    public int runtime { get; set; }
        //    public List<SpokenLanguage> spoken_languages { get; set; }
        //    public string tagline { get; set; }
        //    public string title { get; set; }
        //    public double vote_average { get; set; }
        //    public int vote_count { get; set; }
        //}

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
        /// Class TmdbCastResult
        /// </summary>
        protected class TmdbCastResult
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int id { get; set; }
            /// <summary>
            /// Gets or sets the cast.
            /// </summary>
            /// <value>The cast.</value>
            public List<Cast> cast { get; set; }
            /// <summary>
            /// Gets or sets the crew.
            /// </summary>
            /// <value>The crew.</value>
            public List<Crew> crew { get; set; }
        }

        /// <summary>
        /// Class TmdbReleasesResult
        /// </summary>
        protected class TmdbReleasesResult
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int id { get; set; }
            /// <summary>
            /// Gets or sets the countries.
            /// </summary>
            /// <value>The countries.</value>
            public List<Country> countries { get; set; }
        }

        /// <summary>
        /// Class TmdbImage
        /// </summary>
        protected class TmdbImage
        {
            /// <summary>
            /// Gets or sets the file_path.
            /// </summary>
            /// <value>The file_path.</value>
            public string file_path { get; set; }
            /// <summary>
            /// Gets or sets the width.
            /// </summary>
            /// <value>The width.</value>
            public int width { get; set; }
            /// <summary>
            /// Gets or sets the height.
            /// </summary>
            /// <value>The height.</value>
            public int height { get; set; }
            /// <summary>
            /// Gets or sets the iso_639_1.
            /// </summary>
            /// <value>The iso_639_1.</value>
            public string iso_639_1 { get; set; }
            /// <summary>
            /// Gets or sets the aspect_ratio.
            /// </summary>
            /// <value>The aspect_ratio.</value>
            public double aspect_ratio { get; set; }
            /// <summary>
            /// Gets or sets the vote_average.
            /// </summary>
            /// <value>The vote_average.</value>
            public double vote_average { get; set; }
            /// <summary>
            /// Gets or sets the vote_count.
            /// </summary>
            /// <value>The vote_count.</value>
            public int vote_count { get; set; }
        }

        /// <summary>
        /// Class TmdbImages
        /// </summary>
        protected class TmdbImages
        {
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int id { get; set; }
            /// <summary>
            /// Gets or sets the backdrops.
            /// </summary>
            /// <value>The backdrops.</value>
            public List<TmdbImage> backdrops { get; set; }
            /// <summary>
            /// Gets or sets the posters.
            /// </summary>
            /// <value>The posters.</value>
            public List<TmdbImage> posters { get; set; }
        }

        /// <summary>
        /// Class CompleteMovieData
        /// </summary>
        protected class CompleteMovieData
        {
            public bool adult { get; set; }
            public string backdrop_path { get; set; }
            public BelongsToCollection belongs_to_collection { get; set; }
            public int budget { get; set; }
            public List<Genre> genres { get; set; }
            public string homepage { get; set; }
            public int id { get; set; }
            public string imdb_id { get; set; }
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
            public string tagline { get; set; }
            public string title { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
            public List<Country> countries { get; set; }
            public List<Cast> cast { get; set; }
            public List<Crew> crew { get; set; }
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
    }
}
