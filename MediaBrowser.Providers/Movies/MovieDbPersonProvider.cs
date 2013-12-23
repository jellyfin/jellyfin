using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Movies
{
    /// <summary>
    /// Class TmdbPersonProvider
    /// </summary>
    public class MovieDbPersonProvider : BaseMetadataProvider
    {
        protected readonly IProviderManager ProviderManager;

        internal static MovieDbPersonProvider Current { get; private set; }

        const string DataFileName = "info.json";
        private readonly IFileSystem _fileSystem;

        public MovieDbPersonProvider(IJsonSerializer jsonSerializer, ILogManager logManager, IServerConfigurationManager configurationManager, IProviderManager providerManager, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            JsonSerializer = jsonSerializer;
            ProviderManager = providerManager;
            _fileSystem = fileSystem;
            Current = this;
        }

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        protected IJsonSerializer JsonSerializer { get; private set; }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Person;
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
                return "3";
            }
        }

        public override ItemUpdateType ItemUpdateType
        {
            get
            {
                return ItemUpdateType.MetadataDownload;
            }
        }

        protected override bool NeedsRefreshInternal(BaseItem item, BaseProviderInfo providerInfo)
        {
            if (HasAltMeta(item))
                return false;

            return base.NeedsRefreshInternal(item, providerInfo);
        }

        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var provderId = item.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(provderId))
            {
                // Process images
                var path = GetPersonDataPath(ConfigurationManager.ApplicationPaths, provderId);

                var file = Path.Combine(path, DataFileName);
                var fileInfo = new FileInfo(file);

                if (fileInfo.Exists)
                {
                    return _fileSystem.GetLastWriteTimeUtc(fileInfo) > providerInfo.LastRefreshed;
                }

                return true;
            }

            return base.NeedsRefreshBasedOnCompareDate(item, providerInfo);
        }

        internal static string GetPersonDataPath(IApplicationPaths appPaths, string tmdbId)
        {
            var letter = tmdbId.GetMD5().ToString().Substring(0, 1);

            var seriesDataPath = Path.Combine(GetPersonsDataPath(appPaths), letter, tmdbId);

            return seriesDataPath;
        }

        internal static string GetPersonDataFilePath(IApplicationPaths appPaths, string tmdbId)
        {
            var letter = tmdbId.GetMD5().ToString().Substring(0, 1);

            var seriesDataPath = Path.Combine(GetPersonsDataPath(appPaths), letter, tmdbId);

            return Path.Combine(seriesDataPath, DataFileName);
        }

        internal static string GetPersonsDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.DataPath, "tmdb-people");

            return dataPath;
        }

        private bool HasAltMeta(BaseItem item)
        {
            return item.LocationType == LocationType.FileSystem && item.ResolveArgs.ContainsMetaFileByName("person.xml");
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var person = (Person)item;

            var id = person.GetProviderId(MetadataProviders.Tmdb);

            // We don't already have an Id, need to fetch it
            if (string.IsNullOrEmpty(id))
            {
                id = await GetTmdbId(item, cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!string.IsNullOrEmpty(id))
            {
                await FetchInfo(person, id, force, cancellationToken).ConfigureAwait(false);
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
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

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        /// <summary>
        /// Gets the TMDB id.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> GetTmdbId(BaseItem person, CancellationToken cancellationToken)
        {
            string url = string.Format(@"http://api.themoviedb.org/3/search/person?api_key={1}&query={0}", WebUtility.UrlEncode(person.Name), MovieDbProvider.ApiKey);
            PersonSearchResults searchResult = null;

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = MovieDbProvider.AcceptHeader

            }).ConfigureAwait(false))
            {
                searchResult = JsonSerializer.DeserializeFromStream<PersonSearchResults>(json);
            }

            return searchResult != null && searchResult.Total_Results > 0 ? searchResult.Results[0].Id.ToString(_usCulture) : null;
        }

        /// <summary>
        /// Fetches the info.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="id">The id.</param>
        /// <param name="isForcedRefresh">if set to <c>true</c> [is forced refresh].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task FetchInfo(Person person, string id, bool isForcedRefresh, CancellationToken cancellationToken)
        {
            await DownloadPersonInfoIfNeeded(id, cancellationToken).ConfigureAwait(false);

            if (isForcedRefresh || !HasAltMeta(person))
            {
                var dataFilePath = GetPersonDataFilePath(ConfigurationManager.ApplicationPaths, id);

                var info = JsonSerializer.DeserializeFromFile<PersonResult>(dataFilePath);

                cancellationToken.ThrowIfCancellationRequested();

                ProcessInfo(person, info);
            }
        }

        internal async Task DownloadPersonInfoIfNeeded(string id, CancellationToken cancellationToken)
        {
            var personDataPath = GetPersonDataPath(ConfigurationManager.ApplicationPaths, id);

            var fileInfo = _fileSystem.GetFileSystemInfo(personDataPath);

            if (fileInfo.Exists && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 7)
            {
                return;
            }

            var url = string.Format(@"http://api.themoviedb.org/3/person/{1}?api_key={0}&append_to_response=credits,images", MovieDbProvider.ApiKey, id);

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = MovieDbProvider.AcceptHeader

            }).ConfigureAwait(false))
            {
                Directory.CreateDirectory(personDataPath);

                using (var fs = _fileSystem.GetFileStream(Path.Combine(personDataPath, DataFileName), FileMode.Create, FileAccess.Write, FileShare.Read, true))
                {
                    await json.CopyToAsync(fs).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Processes the info.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="searchResult">The search result.</param>
        protected void ProcessInfo(Person person, PersonResult searchResult)
        {
            if (!person.LockedFields.Contains(MetadataFields.Overview))
            {
                person.Overview = searchResult.biography;
            }

            DateTime date;

            if (DateTime.TryParseExact(searchResult.birthday, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                person.PremiereDate = date.ToUniversalTime();
            }

            if (DateTime.TryParseExact(searchResult.deathday, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out date))
            {
                person.EndDate = date.ToUniversalTime();
            }

            if (!string.IsNullOrEmpty(searchResult.homepage))
            {
                person.HomePageUrl = searchResult.homepage;
            }

            if (!person.LockedFields.Contains(MetadataFields.ProductionLocations))
            {
                if (!string.IsNullOrEmpty(searchResult.place_of_birth))
                {
                    person.PlaceOfBirth = searchResult.place_of_birth;
                }
            }

            person.SetProviderId(MetadataProviders.Tmdb, searchResult.id.ToString(_usCulture));
        }

        #region Result Objects
        /// <summary>
        /// Class PersonSearchResult
        /// </summary>
        public class PersonSearchResult
        {
            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="PersonSearchResult" /> is adult.
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
            public List<PersonSearchResult> Results { get; set; }
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
        }

        #endregion
    }
}
