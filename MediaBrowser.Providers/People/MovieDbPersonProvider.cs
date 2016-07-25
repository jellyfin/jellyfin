using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Movies;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;

namespace MediaBrowser.Providers.People
{
    public class MovieDbPersonProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>
    {
        const string DataFileName = "info.json";

        internal static MovieDbPersonProvider Current { get; private set; }

        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        private int _requestCount;
        private readonly object _requestCountLock = new object();
        private DateTime _lastRequestCountReset;

        public MovieDbPersonProvider(IFileSystem fileSystem, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogger logger)
        {
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _logger = logger;
            Current = this;
        }

        public string Name
        {
            get { return "TheMovieDb"; }
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            var tmdbId = searchInfo.GetProviderId(MetadataProviders.Tmdb);

            var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var tmdbImageUrl = tmdbSettings.images.secure_base_url + "original";

            if (!string.IsNullOrEmpty(tmdbId))
            {
                await EnsurePersonInfo(tmdbId, cancellationToken).ConfigureAwait(false);

                var dataFilePath = GetPersonDataFilePath(_configurationManager.ApplicationPaths, tmdbId);
                var info = _jsonSerializer.DeserializeFromFile<PersonResult>(dataFilePath);

                var images = (info.images ?? new Images()).profiles ?? new List<Profile>();

                var result = new RemoteSearchResult
                {
                    Name = info.name,

                    SearchProviderName = Name,

                    ImageUrl = images.Count == 0 ? null : (tmdbImageUrl + images[0].file_path)
                };

                result.SetProviderId(MetadataProviders.Tmdb, info.id.ToString(_usCulture));
                result.SetProviderId(MetadataProviders.Imdb, info.imdb_id.ToString(_usCulture));

                return new[] { result };
            }

            if (searchInfo.IsAutomated)
            {
                lock (_requestCountLock)
                {
                    if ((DateTime.UtcNow - _lastRequestCountReset).TotalHours >= 1)
                    {
                        _requestCount = 0;
                        _lastRequestCountReset = DateTime.UtcNow;
                    }

                    var requestCount = _requestCount;

                    if (requestCount >= 40)
                    {
                        //_logger.Debug("Throttling Tmdb people");

                        // This needs to be throttled
                        return new List<RemoteSearchResult>();
                    }

                    _requestCount = requestCount + 1;
                }
            }

            var url = string.Format(@"https://api.themoviedb.org/3/search/person?api_key={1}&query={0}", WebUtility.UrlEncode(searchInfo.Name), MovieDbProvider.ApiKey);

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = MovieDbProvider.AcceptHeader

            }).ConfigureAwait(false))
            {
                var result = _jsonSerializer.DeserializeFromStream<PersonSearchResults>(json) ??
                             new PersonSearchResults();

                return result.Results.Select(i => GetSearchResult(i, tmdbImageUrl));
            }
        }

        private RemoteSearchResult GetSearchResult(PersonSearchResult i, string baseImageUrl)
        {
            var result = new RemoteSearchResult
            {
                SearchProviderName = Name,

                Name = i.Name,

                ImageUrl = string.IsNullOrEmpty(i.Profile_Path) ? null : (baseImageUrl + i.Profile_Path)
            };

            result.SetProviderId(MetadataProviders.Tmdb, i.Id.ToString(_usCulture));

            return result;
        }

        public async Task<MetadataResult<Person>> GetMetadata(PersonLookupInfo id, CancellationToken cancellationToken)
        {
            var tmdbId = id.GetProviderId(MetadataProviders.Tmdb);

            // We don't already have an Id, need to fetch it
            if (string.IsNullOrEmpty(tmdbId))
            {
                tmdbId = await GetTmdbId(id, cancellationToken).ConfigureAwait(false);
            }

            var result = new MetadataResult<Person>();

            if (!string.IsNullOrEmpty(tmdbId))
            {
                try
                {
                    await EnsurePersonInfo(tmdbId, cancellationToken).ConfigureAwait(false);
                }
                catch (HttpException ex)
                {
                    if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.NotFound)
                    {
                        return result;
                    }

                    throw;
                }

                var dataFilePath = GetPersonDataFilePath(_configurationManager.ApplicationPaths, tmdbId);

                var info = _jsonSerializer.DeserializeFromFile<PersonResult>(dataFilePath);

                var item = new Person();
                result.HasMetadata = true;

                item.Name = info.name;
                item.HomePageUrl = info.homepage;
                item.PlaceOfBirth = info.place_of_birth;
                item.Overview = info.biography;

                DateTime date;

                if (DateTime.TryParseExact(info.birthday, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out date))
                {
                    item.PremiereDate = date.ToUniversalTime();
                }

                if (DateTime.TryParseExact(info.deathday, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out date))
                {
                    item.EndDate = date.ToUniversalTime();
                }

                item.SetProviderId(MetadataProviders.Tmdb, info.id.ToString(_usCulture));

                if (!string.IsNullOrEmpty(info.imdb_id))
                {
                    item.SetProviderId(MetadataProviders.Imdb, info.imdb_id);
                }

                result.HasMetadata = true;
                result.Item = item;
            }

            return result;
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets the TMDB id.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> GetTmdbId(PersonLookupInfo info, CancellationToken cancellationToken)
        {
            var results = await GetSearchResults(info, cancellationToken).ConfigureAwait(false);

            return results.Select(i => i.GetProviderId(MetadataProviders.Tmdb)).FirstOrDefault();
        }

        internal async Task EnsurePersonInfo(string id, CancellationToken cancellationToken)
        {
            var dataFilePath = GetPersonDataFilePath(_configurationManager.ApplicationPaths, id);

            var fileInfo = _fileSystem.GetFileSystemInfo(dataFilePath);

            if (fileInfo.Exists && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 3)
            {
                return;
            }

            var url = string.Format(@"https://api.themoviedb.org/3/person/{1}?api_key={0}&append_to_response=credits,images,external_ids", MovieDbProvider.ApiKey, id);

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = MovieDbProvider.AcceptHeader

            }).ConfigureAwait(false))
            {
                _fileSystem.CreateDirectory(Path.GetDirectoryName(dataFilePath));

                using (var fs = _fileSystem.GetFileStream(dataFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await json.CopyToAsync(fs).ConfigureAwait(false);
                }
            }
        }

        private static string GetPersonDataPath(IApplicationPaths appPaths, string tmdbId)
        {
            var letter = tmdbId.GetMD5().ToString().Substring(0, 1);

            return Path.Combine(GetPersonsDataPath(appPaths), letter, tmdbId);
        }

        internal static string GetPersonDataFilePath(IApplicationPaths appPaths, string tmdbId)
        {
            return Path.Combine(GetPersonDataPath(appPaths, tmdbId), DataFileName);
        }

        private static string GetPersonsDataPath(IApplicationPaths appPaths)
        {
            return Path.Combine(appPaths.CachePath, "tmdb-people");
        }

        #region Result Objects
        /// <summary>
        /// Class PersonSearchResult
        /// </summary>
        public class PersonSearchResult
        {
            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="MovieDbPersonProvider.PersonSearchResult" /> is adult.
            /// </summary>
            /// <value><c>true</c> if adult; otherwise, <c>false</c>.</value>
            public bool Adult { get; set; }
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int Id { get; set; }
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            /// <value>The name.</value>
            public string Name { get; set; }
            /// <summary>
            /// Gets or sets the profile_ path.
            /// </summary>
            /// <value>The profile_ path.</value>
            public string Profile_Path { get; set; }
        }

        /// <summary>
        /// Class PersonSearchResults
        /// </summary>
        public class PersonSearchResults
        {
            /// <summary>
            /// Gets or sets the page.
            /// </summary>
            /// <value>The page.</value>
            public int Page { get; set; }
            /// <summary>
            /// Gets or sets the results.
            /// </summary>
            /// <value>The results.</value>
            public List<MovieDbPersonProvider.PersonSearchResult> Results { get; set; }
            /// <summary>
            /// Gets or sets the total_ pages.
            /// </summary>
            /// <value>The total_ pages.</value>
            public int Total_Pages { get; set; }
            /// <summary>
            /// Gets or sets the total_ results.
            /// </summary>
            /// <value>The total_ results.</value>
            public int Total_Results { get; set; }
        }

        public class Cast
        {
            public int id { get; set; }
            public string title { get; set; }
            public string character { get; set; }
            public string original_title { get; set; }
            public string poster_path { get; set; }
            public string release_date { get; set; }
            public bool adult { get; set; }
        }

        public class Crew
        {
            public int id { get; set; }
            public string title { get; set; }
            public string original_title { get; set; }
            public string department { get; set; }
            public string job { get; set; }
            public string poster_path { get; set; }
            public string release_date { get; set; }
            public bool adult { get; set; }
        }

        public class Credits
        {
            public List<Cast> cast { get; set; }
            public List<Crew> crew { get; set; }
        }

        public class Profile
        {
            public string file_path { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public object iso_639_1 { get; set; }
            public double aspect_ratio { get; set; }
        }

        public class Images
        {
            public List<Profile> profiles { get; set; }
        }

        public class ExternalIds
        {
            public string imdb_id { get; set; }
            public string freebase_mid { get; set; }
            public string freebase_id { get; set; }
            public int tvrage_id { get; set; }
        }

        public class PersonResult
        {
            public bool adult { get; set; }
            public List<object> also_known_as { get; set; }
            public string biography { get; set; }
            public string birthday { get; set; }
            public string deathday { get; set; }
            public string homepage { get; set; }
            public int id { get; set; }
            public string imdb_id { get; set; }
            public string name { get; set; }
            public string place_of_birth { get; set; }
            public double popularity { get; set; }
            public string profile_path { get; set; }
            public Credits credits { get; set; }
            public Images images { get; set; }
            public ExternalIds external_ids { get; set; }
        }

        #endregion

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url,
                ResourcePool = MovieDbProvider.Current.MovieDbResourcePool
            });
        }
    }
}
