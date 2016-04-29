using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Movies;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Providers.BoxSets
{
    public class MovieDbBoxSetProvider : IRemoteMetadataProvider<BoxSet, BoxSetInfo>
    {
        private const string GetCollectionInfo3 = @"https://api.themoviedb.org/3/collection/{0}?api_key={1}&append_to_response=images";

        internal static MovieDbBoxSetProvider Current;

        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;
        private readonly IHttpClient _httpClient;
        private readonly ILibraryManager _libraryManager;

        public MovieDbBoxSetProvider(ILogger logger, IJsonSerializer json, IServerConfigurationManager config, IFileSystem fileSystem, ILocalizationManager localization, IHttpClient httpClient, ILibraryManager libraryManager)
        {
            _logger = logger;
            _json = json;
            _config = config;
            _fileSystem = fileSystem;
            _localization = localization;
            _httpClient = httpClient;
            _libraryManager = libraryManager;
            Current = this;
        }

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");
        
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BoxSetInfo searchInfo, CancellationToken cancellationToken)
        {
            var tmdbId = searchInfo.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(tmdbId))
            {
                await EnsureInfo(tmdbId, searchInfo.MetadataLanguage, cancellationToken).ConfigureAwait(false);

                var dataFilePath = GetDataFilePath(_config.ApplicationPaths, tmdbId, searchInfo.MetadataLanguage);
                var info = _json.DeserializeFromFile<RootObject>(dataFilePath);

                var images = (info.images ?? new Images()).posters ?? new List<Poster>();

                var tmdbSettings = await MovieDbProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

                var tmdbImageUrl = tmdbSettings.images.secure_base_url + "original";

                var result = new RemoteSearchResult
                {
                    Name = info.name,

                    SearchProviderName = Name,
                    
                    ImageUrl = images.Count == 0 ? null : (tmdbImageUrl + images[0].file_path)
                };

                result.SetProviderId(MetadataProviders.Tmdb, info.id.ToString(_usCulture));

                return new[] { result };
            }

            return await new MovieDbSearch(_logger, _json, _libraryManager).GetSearchResults(searchInfo, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MetadataResult<BoxSet>> GetMetadata(BoxSetInfo id, CancellationToken cancellationToken)
        {
            var tmdbId = id.GetProviderId(MetadataProviders.Tmdb);

            // We don't already have an Id, need to fetch it
            if (string.IsNullOrEmpty(tmdbId))
            {
                var searchResults = await new MovieDbSearch(_logger, _json, _libraryManager).GetSearchResults(id, cancellationToken).ConfigureAwait(false);

                var searchResult = searchResults.FirstOrDefault();

                if (searchResult != null)
                {
                    tmdbId = searchResult.GetProviderId(MetadataProviders.Tmdb);
                }
            }

            var result = new MetadataResult<BoxSet>();

            if (!string.IsNullOrEmpty(tmdbId))
            {
                var mainResult = await GetMovieDbResult(tmdbId, id.MetadataLanguage, cancellationToken).ConfigureAwait(false);

                if (mainResult != null)
                {
                    result.HasMetadata = true;
                    result.Item = GetItem(mainResult);
                }
            }

            return result;
        }

        internal async Task<RootObject> GetMovieDbResult(string tmdbId, string language, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException("tmdbId");
            }

            await EnsureInfo(tmdbId, language, cancellationToken).ConfigureAwait(false);

            var dataFilePath = GetDataFilePath(_config.ApplicationPaths, tmdbId, language);

            if (!string.IsNullOrEmpty(dataFilePath))
            {
                return _json.DeserializeFromFile<RootObject>(dataFilePath);
            }

            return null;
        }

        private BoxSet GetItem(RootObject obj)
        {
            var item = new BoxSet
            {
                Name = obj.name,
                Overview = obj.overview
            };

            item.SetProviderId(MetadataProviders.Tmdb, obj.id.ToString(_usCulture));

            return item;
        }

        private async Task DownloadInfo(string tmdbId, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var mainResult = await FetchMainResult(tmdbId, preferredMetadataLanguage, cancellationToken).ConfigureAwait(false);

            if (mainResult == null) return;

            var dataFilePath = GetDataFilePath(_config.ApplicationPaths, tmdbId, preferredMetadataLanguage);

			_fileSystem.CreateDirectory(Path.GetDirectoryName(dataFilePath));

            _json.SerializeToFile(mainResult, dataFilePath);
        }

        private async Task<RootObject> FetchMainResult(string id, string language, CancellationToken cancellationToken)
        {
            var url = string.Format(GetCollectionInfo3, id, MovieDbProvider.ApiKey);

            if (!string.IsNullOrEmpty(language))
            {
                url += string.Format("&language={0}", MovieDbProvider.NormalizeLanguage(language));

                // Get images in english and with no language
                url += "&include_image_language=" + MovieDbProvider.GetImageLanguagesParam(language);
            }

            cancellationToken.ThrowIfCancellationRequested();

            RootObject mainResult = null;

            using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = MovieDbSearch.AcceptHeader

            }).ConfigureAwait(false))
            {
                mainResult = _json.DeserializeFromStream<RootObject>(json);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (mainResult != null && string.IsNullOrEmpty(mainResult.name))
            {
                if (!string.IsNullOrEmpty(language) && !string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
                {
                    url = string.Format(GetCollectionInfo3, id, MovieDbSearch.ApiKey) + "&language=en";

                    if (!string.IsNullOrEmpty(language))
                    {
                        // Get images in english and with no language
                        url += "&include_image_language=" + MovieDbProvider.GetImageLanguagesParam(language);
                    }

                    using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
                    {
                        Url = url,
                        CancellationToken = cancellationToken,
                        AcceptHeader = MovieDbSearch.AcceptHeader

                    }).ConfigureAwait(false))
                    {
                        mainResult = _json.DeserializeFromStream<RootObject>(json);
                    }
                }
            }
            return mainResult;
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        internal Task EnsureInfo(string tmdbId, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var path = GetDataFilePath(_config.ApplicationPaths, tmdbId, preferredMetadataLanguage);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 3)
                {
                    return _cachedTask;
                }
            }

            return DownloadInfo(tmdbId, preferredMetadataLanguage, cancellationToken);
        }

        public string Name
        {
            get { return "TheMovieDb"; }
        }

        private static string GetDataFilePath(IApplicationPaths appPaths, string tmdbId, string preferredLanguage)
        {
            var path = GetDataPath(appPaths, tmdbId);

            var filename = string.Format("all-{0}.json", preferredLanguage ?? string.Empty);

            return Path.Combine(path, filename);
        }

        private static string GetDataPath(IApplicationPaths appPaths, string tmdbId)
        {
            var dataPath = GetCollectionsDataPath(appPaths);

            return Path.Combine(dataPath, tmdbId);
        }

        private static string GetCollectionsDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.CachePath, "tmdb-collections");

            return dataPath;
        }

        internal class Part
        {
            public string title { get; set; }
            public int id { get; set; }
            public string release_date { get; set; }
            public string poster_path { get; set; }
            public string backdrop_path { get; set; }
        }

        internal class Backdrop
        {
            public double aspect_ratio { get; set; }
            public string file_path { get; set; }
            public int height { get; set; }
            public string iso_639_1 { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
            public int width { get; set; }
        }

        internal class Poster
        {
            public double aspect_ratio { get; set; }
            public string file_path { get; set; }
            public int height { get; set; }
            public string iso_639_1 { get; set; }
            public double vote_average { get; set; }
            public int vote_count { get; set; }
            public int width { get; set; }
        }

        internal class Images
        {
            public List<Backdrop> backdrops { get; set; }
            public List<Poster> posters { get; set; }
        }

        internal class RootObject
        {
            public int id { get; set; }
            public string name { get; set; }
            public string overview { get; set; }
            public string poster_path { get; set; }
            public string backdrop_path { get; set; }
            public List<Part> parts { get; set; }
            public Images images { get; set; }
        }

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
