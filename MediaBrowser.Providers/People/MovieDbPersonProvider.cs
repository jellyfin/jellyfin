using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Movies;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.People
{
    public class MovieDbPersonProvider : IRemoteMetadataProvider<Person>
    {
        const string DataFileName = "info.json";
        
        internal static MovieDbPersonProvider Current { get; private set; }
        
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;

        public MovieDbPersonProvider(IFileSystem fileSystem, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer)
        {
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _jsonSerializer = jsonSerializer;
            Current = this;
        }

        public string Name
        {
            get { return "TheMovieDb"; }
        }
        
        public async Task<MetadataResult<Person>> GetMetadata(ItemId id, CancellationToken cancellationToken)
        {
            var tmdbId = id.GetProviderId(MetadataProviders.Tmdb);

            // We don't already have an Id, need to fetch it
            if (string.IsNullOrEmpty(tmdbId))
            {
                tmdbId = await GetTmdbId(id.Name, cancellationToken).ConfigureAwait(false);
            }

            var result = new MetadataResult<Person>();

            if (!string.IsNullOrEmpty(tmdbId))
            {
                await EnsurePersonInfo(tmdbId, cancellationToken).ConfigureAwait(false);

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
        /// <param name="name">The name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.String}.</returns>
        private async Task<string> GetTmdbId(string name, CancellationToken cancellationToken)
        {
            string url = string.Format(@"http://api.themoviedb.org/3/search/person?api_key={1}&query={0}", WebUtility.UrlEncode(name), MovieDbProvider.ApiKey);
            PersonSearchResults searchResult = null;

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = MovieDbProvider.AcceptHeader

            }).ConfigureAwait(false))
            {
                searchResult = _jsonSerializer.DeserializeFromStream<PersonSearchResults>(json);
            }

            return searchResult != null && searchResult.Total_Results > 0 ? searchResult.Results[0].Id.ToString(_usCulture) : null;
        }

        internal async Task EnsurePersonInfo(string id, CancellationToken cancellationToken)
        {
            var dataFilePath = GetPersonDataFilePath(_configurationManager.ApplicationPaths, id);

            var fileInfo = _fileSystem.GetFileSystemInfo(dataFilePath);

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
                Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath));

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
            return Path.Combine(appPaths.DataPath, "tmdb-people");
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
