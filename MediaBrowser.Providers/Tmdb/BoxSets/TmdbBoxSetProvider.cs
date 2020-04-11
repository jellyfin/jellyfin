using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Tmdb.Models.Collections;
using MediaBrowser.Providers.Tmdb.Models.General;
using MediaBrowser.Providers.Tmdb.Movies;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Tmdb.BoxSets
{
    public class TmdbBoxSetProvider : IRemoteMetadataProvider<BoxSet, BoxSetInfo>
    {
        private const string GetCollectionInfo3 = TmdbUtils.BaseTmdbApiUrl + @"3/collection/{0}?api_key={1}&append_to_response=images";

        internal static TmdbBoxSetProvider Current;

        private readonly ILogger _logger;
        private readonly IJsonSerializer _json;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;
        private readonly IHttpClient _httpClient;
        private readonly ILibraryManager _libraryManager;

        public TmdbBoxSetProvider(
            ILogger<TmdbBoxSetProvider> logger,
            IJsonSerializer json,
            IServerConfigurationManager config,
            IFileSystem fileSystem,
            ILocalizationManager localization,
            IHttpClient httpClient,
            ILibraryManager libraryManager)
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
                var info = _json.DeserializeFromFile<CollectionResult>(dataFilePath);

                var images = (info.Images ?? new CollectionImages()).Posters ?? new List<Poster>();

                var tmdbSettings = await TmdbMovieProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

                var tmdbImageUrl = tmdbSettings.images.GetImageUrl("original");

                var result = new RemoteSearchResult
                {
                    Name = info.Name,

                    SearchProviderName = Name,

                    ImageUrl = images.Count == 0 ? null : (tmdbImageUrl + images[0].File_Path)
                };

                result.SetProviderId(MetadataProviders.Tmdb, info.Id.ToString(_usCulture));

                return new[] { result };
            }

            return await new TmdbSearch(_logger, _json, _libraryManager).GetSearchResults(searchInfo, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MetadataResult<BoxSet>> GetMetadata(BoxSetInfo id, CancellationToken cancellationToken)
        {
            var tmdbId = id.GetProviderId(MetadataProviders.Tmdb);

            // We don't already have an Id, need to fetch it
            if (string.IsNullOrEmpty(tmdbId))
            {
                var searchResults = await new TmdbSearch(_logger, _json, _libraryManager).GetSearchResults(id, cancellationToken).ConfigureAwait(false);

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

        internal async Task<CollectionResult> GetMovieDbResult(string tmdbId, string language, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException(nameof(tmdbId));
            }

            await EnsureInfo(tmdbId, language, cancellationToken).ConfigureAwait(false);

            var dataFilePath = GetDataFilePath(_config.ApplicationPaths, tmdbId, language);

            if (!string.IsNullOrEmpty(dataFilePath))
            {
                return _json.DeserializeFromFile<CollectionResult>(dataFilePath);
            }

            return null;
        }

        private BoxSet GetItem(CollectionResult obj)
        {
            var item = new BoxSet
            {
                Name = obj.Name,
                Overview = obj.Overview
            };

            item.SetProviderId(MetadataProviders.Tmdb, obj.Id.ToString(_usCulture));

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

        private async Task<CollectionResult> FetchMainResult(string id, string language, CancellationToken cancellationToken)
        {
            var url = string.Format(GetCollectionInfo3, id, TmdbUtils.ApiKey);

            if (!string.IsNullOrEmpty(language))
            {
                url += string.Format("&language={0}", TmdbMovieProvider.NormalizeLanguage(language));

                // Get images in english and with no language
                url += "&include_image_language=" + TmdbMovieProvider.GetImageLanguagesParam(language);
            }

            cancellationToken.ThrowIfCancellationRequested();

            CollectionResult mainResult;

            using (var response = await TmdbMovieProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = TmdbUtils.AcceptHeader

            }).ConfigureAwait(false))
            {
                using (var json = response.Content)
                {
                    mainResult = await _json.DeserializeFromStreamAsync<CollectionResult>(json).ConfigureAwait(false);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (mainResult != null && string.IsNullOrEmpty(mainResult.Name))
            {
                if (!string.IsNullOrEmpty(language) && !string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
                {
                    url = string.Format(GetCollectionInfo3, id, TmdbUtils.ApiKey) + "&language=en";

                    if (!string.IsNullOrEmpty(language))
                    {
                        // Get images in english and with no language
                        url += "&include_image_language=" + TmdbMovieProvider.GetImageLanguagesParam(language);
                    }

                    using (var response = await TmdbMovieProvider.Current.GetMovieDbResponse(new HttpRequestOptions
                    {
                        Url = url,
                        CancellationToken = cancellationToken,
                        AcceptHeader = TmdbUtils.AcceptHeader

                    }).ConfigureAwait(false))
                    {
                        using (var json = response.Content)
                        {
                            mainResult = await _json.DeserializeFromStreamAsync<CollectionResult>(json).ConfigureAwait(false);
                        }
                    }
                }
            }
            return mainResult;
        }

        internal Task EnsureInfo(string tmdbId, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var path = GetDataFilePath(_config.ApplicationPaths, tmdbId, preferredMetadataLanguage);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 2)
                {
                    return Task.CompletedTask;
                }
            }

            return DownloadInfo(tmdbId, preferredMetadataLanguage, cancellationToken);
        }

        public string Name => TmdbUtils.ProviderName;

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

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }
    }
}
