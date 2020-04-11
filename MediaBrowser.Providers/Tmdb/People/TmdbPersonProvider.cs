using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Tmdb.Models.General;
using MediaBrowser.Providers.Tmdb.Models.People;
using MediaBrowser.Providers.Tmdb.Models.Search;
using MediaBrowser.Providers.Tmdb.Movies;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Tmdb.People
{
    public class TmdbPersonProvider : IRemoteMetadataProvider<Person, PersonLookupInfo>
    {
        const string DataFileName = "info.json";

        internal static TmdbPersonProvider Current { get; private set; }

        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public TmdbPersonProvider(
            IFileSystem fileSystem,
            IServerConfigurationManager configurationManager,
            IJsonSerializer jsonSerializer,
            IHttpClient httpClient,
            ILogger<TmdbPersonProvider> logger)
        {
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _jsonSerializer = jsonSerializer;
            _httpClient = httpClient;
            _logger = logger;
            Current = this;
        }

        public string Name => TmdbUtils.ProviderName;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(PersonLookupInfo searchInfo, CancellationToken cancellationToken)
        {
            var tmdbId = searchInfo.GetProviderId(MetadataProviders.Tmdb);

            var tmdbSettings = await TmdbMovieProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            var tmdbImageUrl = tmdbSettings.images.GetImageUrl("original");

            if (!string.IsNullOrEmpty(tmdbId))
            {
                await EnsurePersonInfo(tmdbId, cancellationToken).ConfigureAwait(false);

                var dataFilePath = GetPersonDataFilePath(_configurationManager.ApplicationPaths, tmdbId);
                var info = _jsonSerializer.DeserializeFromFile<PersonResult>(dataFilePath);

                var images = (info.Images ?? new PersonImages()).Profiles ?? new List<Profile>();

                var result = new RemoteSearchResult
                {
                    Name = info.Name,

                    SearchProviderName = Name,

                    ImageUrl = images.Count == 0 ? null : (tmdbImageUrl + images[0].File_Path)
                };

                result.SetProviderId(MetadataProviders.Tmdb, info.Id.ToString(_usCulture));
                result.SetProviderId(MetadataProviders.Imdb, info.Imdb_Id);

                return new[] { result };
            }

            if (searchInfo.IsAutomated)
            {
                // Don't hammer moviedb searching by name
                return new List<RemoteSearchResult>();
            }

            var url = string.Format(TmdbUtils.BaseTmdbApiUrl + @"3/search/person?api_key={1}&query={0}", WebUtility.UrlEncode(searchInfo.Name), TmdbUtils.ApiKey);

            using (var response = await TmdbMovieProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = TmdbUtils.AcceptHeader

            }).ConfigureAwait(false))
            {
                using (var json = response.Content)
                {
                    var result = await _jsonSerializer.DeserializeFromStreamAsync<TmdbSearchResult<PersonSearchResult>>(json).ConfigureAwait(false) ??
                                 new TmdbSearchResult<PersonSearchResult>();

                    return result.Results.Select(i => GetSearchResult(i, tmdbImageUrl));
                }
            }
        }

        private RemoteSearchResult GetSearchResult(PersonSearchResult i, string baseImageUrl)
        {
            var result = new RemoteSearchResult
            {
                SearchProviderName = Name,

                Name = i.Name,

                ImageUrl = string.IsNullOrEmpty(i.Profile_Path) ? null : baseImageUrl + i.Profile_Path
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

                // Take name from incoming info, don't rename the person
                // TODO: This should go in PersonMetadataService, not each person provider
                item.Name = id.Name;

                //item.HomePageUrl = info.homepage;

                if (!string.IsNullOrWhiteSpace(info.Place_Of_Birth))
                {
                    item.ProductionLocations = new string[] { info.Place_Of_Birth };
                }
                item.Overview = info.Biography;

                if (DateTime.TryParseExact(info.Birthday, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out var date))
                {
                    item.PremiereDate = date.ToUniversalTime();
                }

                if (DateTime.TryParseExact(info.Deathday, "yyyy-MM-dd", new CultureInfo("en-US"), DateTimeStyles.None, out date))
                {
                    item.EndDate = date.ToUniversalTime();
                }

                item.SetProviderId(MetadataProviders.Tmdb, info.Id.ToString(_usCulture));

                if (!string.IsNullOrEmpty(info.Imdb_Id))
                {
                    item.SetProviderId(MetadataProviders.Imdb, info.Imdb_Id);
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

            if (fileInfo.Exists && (DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 2)
            {
                return;
            }

            var url = string.Format(TmdbUtils.BaseTmdbApiUrl + @"3/person/{1}?api_key={0}&append_to_response=credits,images,external_ids", TmdbUtils.ApiKey, id);

            using (var response = await TmdbMovieProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = TmdbUtils.AcceptHeader

            }).ConfigureAwait(false))
            {
                using (var json = response.Content)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath));

                    using (var fs = new FileStream(dataFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, IODefaults.FileStreamBufferSize, true))
                    {
                        await json.CopyToAsync(fs).ConfigureAwait(false);
                    }
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
