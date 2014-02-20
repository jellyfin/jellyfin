using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Movies;
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

namespace MediaBrowser.Providers.BoxSets
{
    public class MovieDbBoxSetProvider : IRemoteMetadataProvider<BoxSet, BoxSetInfo>
    {
        private readonly CultureInfo _enUs = new CultureInfo("en-US");
        private const string GetCollectionInfo3 = @"http://api.themoviedb.org/3/collection/{0}?api_key={1}&append_to_response=images";

        internal static MovieDbBoxSetProvider Current;

        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;

        public MovieDbBoxSetProvider(ILogger logger, IJsonSerializer json, IServerConfigurationManager config, IFileSystem fileSystem, ILocalizationManager localization)
        {
            _logger = logger;
            _json = json;
            _config = config;
            _fileSystem = fileSystem;
            _localization = localization;
            Current = this;
        }

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BoxSetInfo searchInfo, CancellationToken cancellationToken)
        {
            return new List<RemoteSearchResult>();
        }

        public async Task<MetadataResult<BoxSet>> GetMetadata(BoxSetInfo id, CancellationToken cancellationToken)
        {
            var tmdbId = id.GetProviderId(MetadataProviders.Tmdb);

            // We don't already have an Id, need to fetch it
            if (string.IsNullOrEmpty(tmdbId))
            {
                var searchResult = await new MovieDbSearch(_logger, _json).FindCollectionId(id, cancellationToken).ConfigureAwait(false);

                if (searchResult != null)
                {
                    tmdbId = searchResult.id.ToString(_enUs);
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

            item.SetProviderId(MetadataProviders.Tmdb, obj.id.ToString(_enUs));

            return item;
        }

        private async Task DownloadInfo(string tmdbId, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var mainResult = await FetchMainResult(tmdbId, preferredMetadataLanguage, cancellationToken).ConfigureAwait(false);

            if (mainResult == null) return;

            var dataFilePath = GetDataFilePath(_config.ApplicationPaths, tmdbId, preferredMetadataLanguage);

            Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath));

            _json.SerializeToFile(mainResult, dataFilePath);
        }

        private async Task<RootObject> FetchMainResult(string id, string language, CancellationToken cancellationToken)
        {
            var url = string.Format(GetCollectionInfo3, id, MovieDbProvider.ApiKey);

            var imageLanguages = _localization.GetCultures()
                .Select(i => i.TwoLetterISOLanguageName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            imageLanguages.Add("null");

            if (!string.IsNullOrEmpty(language))
            {
                // If preferred language isn't english, get those images too
                if (imageLanguages.Contains(language, StringComparer.OrdinalIgnoreCase))
                {
                    imageLanguages.Add(language);
                }

                url += string.Format("&language={0}", language);
            }

            // Get images in english and with no language
            url += "&include_image_language=" + string.Join(",", imageLanguages.ToArray());

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

            if (mainResult != null && string.IsNullOrEmpty(mainResult.overview))
            {
                if (!string.IsNullOrEmpty(language) && !string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
                {
                    url = string.Format(GetCollectionInfo3, id, MovieDbSearch.ApiKey) + "&include_image_language=en,null&language=en";

                    using (var json = await MovieDbProvider.Current.GetMovieDbResponse(new HttpRequestOptions
                    {
                        Url = url,
                        CancellationToken = cancellationToken,
                        AcceptHeader = MovieDbSearch.AcceptHeader

                    }).ConfigureAwait(false))
                    {
                        mainResult = _json.DeserializeFromStream<RootObject>(json);
                    }

                    if (String.IsNullOrEmpty(mainResult.overview))
                    {
                        _logger.Error("Unable to find information for (id:" + id + ")");
                        return null;
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
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 7)
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

            var filename = string.Format("all-{0}.json",
                preferredLanguage ?? string.Empty);

            return Path.Combine(path, filename);
        }

        private static string GetDataPath(IApplicationPaths appPaths, string tmdbId)
        {
            var dataPath = GetCollectionsDataPath(appPaths);

            return Path.Combine(dataPath, tmdbId);
        }

        private static string GetCollectionsDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.DataPath, "tmdb-collections");

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
            throw new NotImplementedException();
        }
    }
}
