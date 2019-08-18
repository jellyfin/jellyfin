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
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Movies;
using MediaBrowser.Providers.Tmdb.Models.Search;
using MediaBrowser.Providers.Tmdb.Models.TV;
using MediaBrowser.Providers.Tmdb.Movies;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Tmdb.TV
{
    public class TmdbSeriesProvider : IRemoteMetadataProvider<Series, SeriesInfo>, IHasOrder
    {
        private const string GetTvInfo3 = TmdbUtils.BaseMovieDbUrl + @"3/tv/{0}?api_key={1}&append_to_response=credits,images,keywords,external_ids,videos,content_ratings";
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        internal static TmdbSeriesProvider Current { get; private set; }

        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILogger _logger;
        private readonly ILocalizationManager _localization;
        private readonly IHttpClient _httpClient;
        private readonly ILibraryManager _libraryManager;

        public TmdbSeriesProvider(IJsonSerializer jsonSerializer, IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILogger logger, ILocalizationManager localization, IHttpClient httpClient, ILibraryManager libraryManager)
        {
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _configurationManager = configurationManager;
            _logger = logger;
            _localization = localization;
            _httpClient = httpClient;
            _libraryManager = libraryManager;
            Current = this;
        }

        public string Name => TmdbUtils.ProviderName;

        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeriesInfo searchInfo, CancellationToken cancellationToken)
        {
            var tmdbId = searchInfo.GetProviderId(MetadataProviders.Tmdb);

            if (!string.IsNullOrEmpty(tmdbId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                await EnsureSeriesInfo(tmdbId, searchInfo.MetadataLanguage, cancellationToken).ConfigureAwait(false);

                var dataFilePath = GetDataFilePath(tmdbId, searchInfo.MetadataLanguage);

                var obj = _jsonSerializer.DeserializeFromFile<SeriesResult>(dataFilePath);

                var tmdbSettings = await TmdbMovieProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);
                var tmdbImageUrl = tmdbSettings.images.GetImageUrl("original");

                var remoteResult = new RemoteSearchResult
                {
                    Name = obj.name,
                    SearchProviderName = Name,
                    ImageUrl = string.IsNullOrWhiteSpace(obj.poster_path) ? null : tmdbImageUrl + obj.poster_path
                };

                remoteResult.SetProviderId(MetadataProviders.Tmdb, obj.id.ToString(_usCulture));
                remoteResult.SetProviderId(MetadataProviders.Imdb, obj.external_ids.imdb_id);

                if (obj.external_ids.tvdb_id > 0)
                {
                    remoteResult.SetProviderId(MetadataProviders.Tvdb, obj.external_ids.tvdb_id.ToString(_usCulture));
                }

                return new[] { remoteResult };
            }

            var imdbId = searchInfo.GetProviderId(MetadataProviders.Imdb);

            if (!string.IsNullOrEmpty(imdbId))
            {
                var searchResult = await FindByExternalId(imdbId, "imdb_id", cancellationToken).ConfigureAwait(false);

                if (searchResult != null)
                {
                    return new[] { searchResult };
                }
            }

            var tvdbId = searchInfo.GetProviderId(MetadataProviders.Tvdb);

            if (!string.IsNullOrEmpty(tvdbId))
            {
                var searchResult = await FindByExternalId(tvdbId, "tvdb_id", cancellationToken).ConfigureAwait(false);

                if (searchResult != null)
                {
                    return new[] { searchResult };
                }
            }

            return await new TmdbSearch(_logger, _jsonSerializer, _libraryManager).GetSearchResults(searchInfo, cancellationToken).ConfigureAwait(false);
        }

        public async Task<MetadataResult<Series>> GetMetadata(SeriesInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Series>();
            result.QueriedById = true;

            var tmdbId = info.GetProviderId(MetadataProviders.Tmdb);

            if (string.IsNullOrEmpty(tmdbId))
            {
                var imdbId = info.GetProviderId(MetadataProviders.Imdb);

                if (!string.IsNullOrEmpty(imdbId))
                {
                    var searchResult = await FindByExternalId(imdbId, "imdb_id", cancellationToken).ConfigureAwait(false);

                    if (searchResult != null)
                    {
                        tmdbId = searchResult.GetProviderId(MetadataProviders.Tmdb);
                    }
                }
            }

            if (string.IsNullOrEmpty(tmdbId))
            {
                var tvdbId = info.GetProviderId(MetadataProviders.Tvdb);

                if (!string.IsNullOrEmpty(tvdbId))
                {
                    var searchResult = await FindByExternalId(tvdbId, "tvdb_id", cancellationToken).ConfigureAwait(false);

                    if (searchResult != null)
                    {
                        tmdbId = searchResult.GetProviderId(MetadataProviders.Tmdb);
                    }
                }
            }

            if (string.IsNullOrEmpty(tmdbId))
            {
                result.QueriedById = false;
                var searchResults = await new TmdbSearch(_logger, _jsonSerializer, _libraryManager).GetSearchResults(info, cancellationToken).ConfigureAwait(false);

                var searchResult = searchResults.FirstOrDefault();

                if (searchResult != null)
                {
                    tmdbId = searchResult.GetProviderId(MetadataProviders.Tmdb);
                }
            }

            if (!string.IsNullOrEmpty(tmdbId))
            {
                cancellationToken.ThrowIfCancellationRequested();

                result = await FetchMovieData(tmdbId, info.MetadataLanguage, info.MetadataCountryCode, cancellationToken).ConfigureAwait(false);

                result.HasMetadata = result.Item != null;
            }

            return result;
        }

        private async Task<MetadataResult<Series>> FetchMovieData(string tmdbId, string language, string preferredCountryCode, CancellationToken cancellationToken)
        {
            SeriesResult seriesInfo = await FetchMainResult(tmdbId, language, cancellationToken).ConfigureAwait(false);

            if (seriesInfo == null)
            {
                return null;
            }

            tmdbId = seriesInfo.id.ToString(_usCulture);

            string dataFilePath = GetDataFilePath(tmdbId, language);
            Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath));
            _jsonSerializer.SerializeToFile(seriesInfo, dataFilePath);

            await EnsureSeriesInfo(tmdbId, language, cancellationToken).ConfigureAwait(false);

            var result = new MetadataResult<Series>();
            result.Item = new Series();
            result.ResultLanguage = seriesInfo.ResultLanguage;

            var settings = await TmdbMovieProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);

            ProcessMainInfo(result, seriesInfo, preferredCountryCode, settings);

            return result;
        }

        private void ProcessMainInfo(MetadataResult<Series> seriesResult, SeriesResult seriesInfo, string preferredCountryCode, TmdbSettingsResult settings)
        {
            var series = seriesResult.Item;

            series.Name = seriesInfo.name;
            series.SetProviderId(MetadataProviders.Tmdb, seriesInfo.id.ToString(_usCulture));

            //series.VoteCount = seriesInfo.vote_count;

            string voteAvg = seriesInfo.vote_average.ToString(CultureInfo.InvariantCulture);

            if (float.TryParse(voteAvg, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float rating))
            {
                series.CommunityRating = rating;
            }

            series.Overview = seriesInfo.overview;

            if (seriesInfo.networks != null)
            {
                series.Studios = seriesInfo.networks.Select(i => i.name).ToArray();
            }

            if (seriesInfo.genres != null)
            {
                series.Genres = seriesInfo.genres.Select(i => i.name).ToArray();
            }

            //series.HomePageUrl = seriesInfo.homepage;

            series.RunTimeTicks = seriesInfo.episode_run_time.Select(i => TimeSpan.FromMinutes(i).Ticks).FirstOrDefault();

            if (string.Equals(seriesInfo.status, "Ended", StringComparison.OrdinalIgnoreCase))
            {
                series.Status = SeriesStatus.Ended;
                series.EndDate = seriesInfo.last_air_date;
            }
            else
            {
                series.Status = SeriesStatus.Continuing;
            }

            series.PremiereDate = seriesInfo.first_air_date;

            var ids = seriesInfo.external_ids;
            if (ids != null)
            {
                if (!string.IsNullOrWhiteSpace(ids.imdb_id))
                {
                    series.SetProviderId(MetadataProviders.Imdb, ids.imdb_id);
                }
                if (ids.tvrage_id > 0)
                {
                    series.SetProviderId(MetadataProviders.TvRage, ids.tvrage_id.ToString(_usCulture));
                }
                if (ids.tvdb_id > 0)
                {
                    series.SetProviderId(MetadataProviders.Tvdb, ids.tvdb_id.ToString(_usCulture));
                }
            }

            var contentRatings = (seriesInfo.content_ratings ?? new ContentRatings()).results ?? new List<ContentRating>();

            var ourRelease = contentRatings.FirstOrDefault(c => string.Equals(c.iso_3166_1, preferredCountryCode, StringComparison.OrdinalIgnoreCase));
            var usRelease = contentRatings.FirstOrDefault(c => string.Equals(c.iso_3166_1, "US", StringComparison.OrdinalIgnoreCase));
            var minimumRelease = contentRatings.FirstOrDefault();

            if (ourRelease != null)
            {
                series.OfficialRating = ourRelease.rating;
            }
            else if (usRelease != null)
            {
                series.OfficialRating = usRelease.rating;
            }
            else if (minimumRelease != null)
            {
                series.OfficialRating = minimumRelease.rating;
            }

            if (seriesInfo.videos != null && seriesInfo.videos.results != null)
            {
                foreach (var video in seriesInfo.videos.results)
                {
                    if ((video.type.Equals("trailer", StringComparison.OrdinalIgnoreCase)
                        || video.type.Equals("clip", StringComparison.OrdinalIgnoreCase))
                        && video.site.Equals("youtube", StringComparison.OrdinalIgnoreCase))
                    {
                        series.AddTrailerUrl($"http://www.youtube.com/watch?v={video.key}");
                    }
                }
            }

            seriesResult.ResetPeople();
            var tmdbImageUrl = settings.images.GetImageUrl("original");

            if (seriesInfo.credits != null && seriesInfo.credits.cast != null)
            {
                foreach (var actor in seriesInfo.credits.cast.OrderBy(a => a.order))
                {
                    var personInfo = new PersonInfo
                    {
                        Name = actor.name.Trim(),
                        Role = actor.character,
                        Type = PersonType.Actor,
                        SortOrder = actor.order
                    };

                    if (!string.IsNullOrWhiteSpace(actor.profile_path))
                    {
                        personInfo.ImageUrl = tmdbImageUrl + actor.profile_path;
                    }

                    if (actor.id > 0)
                    {
                        personInfo.SetProviderId(MetadataProviders.Tmdb, actor.id.ToString(CultureInfo.InvariantCulture));
                    }

                    seriesResult.AddPerson(personInfo);
                }
            }
        }

        internal static string GetSeriesDataPath(IApplicationPaths appPaths, string tmdbId)
        {
            var dataPath = GetSeriesDataPath(appPaths);

            return Path.Combine(dataPath, tmdbId);
        }

        internal static string GetSeriesDataPath(IApplicationPaths appPaths)
        {
            var dataPath = Path.Combine(appPaths.CachePath, "tmdb-tv");

            return dataPath;
        }

        internal async Task DownloadSeriesInfo(string id, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            SeriesResult mainResult = await FetchMainResult(id, preferredMetadataLanguage, cancellationToken).ConfigureAwait(false);

            if (mainResult == null)
            {
                return;
            }

            var dataFilePath = GetDataFilePath(id, preferredMetadataLanguage);

            Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath));

            _jsonSerializer.SerializeToFile(mainResult, dataFilePath);
        }

        internal async Task<SeriesResult> FetchMainResult(string id, string language, CancellationToken cancellationToken)
        {
            var url = string.Format(GetTvInfo3, id, TmdbUtils.ApiKey);

            if (!string.IsNullOrEmpty(language))
            {
                url += "&language=" + TmdbMovieProvider.NormalizeLanguage(language)
                    + "&include_image_language=" + TmdbMovieProvider.GetImageLanguagesParam(language); // Get images in english and with no language
            }

            cancellationToken.ThrowIfCancellationRequested();

            SeriesResult mainResult;

            using (var response = await TmdbMovieProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = TmdbUtils.AcceptHeader

            }).ConfigureAwait(false))
            {
                using (var json = response.Content)
                {
                    mainResult = await _jsonSerializer.DeserializeFromStreamAsync<SeriesResult>(json).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(language))
                    {
                        mainResult.ResultLanguage = language;
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // If the language preference isn't english, then have the overview fallback to english if it's blank
            if (mainResult != null &&
                string.IsNullOrEmpty(mainResult.overview) &&
                !string.IsNullOrEmpty(language) &&
                !string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("MovieDbSeriesProvider couldn't find meta for language {Language}. Trying English...", language);

                url = string.Format(GetTvInfo3, id, TmdbUtils.ApiKey) + "&language=en";

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
                        var englishResult = await _jsonSerializer.DeserializeFromStreamAsync<SeriesResult>(json).ConfigureAwait(false);

                        mainResult.overview = englishResult.overview;
                        mainResult.ResultLanguage = "en";
                    }
                }
            }

            return mainResult;
        }

        internal Task EnsureSeriesInfo(string tmdbId, string language, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException(nameof(tmdbId));
            }

            var path = GetDataFilePath(tmdbId, language);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 2)
                {
                    return Task.CompletedTask;
                }
            }

            return DownloadSeriesInfo(tmdbId, language, cancellationToken);
        }

        internal string GetDataFilePath(string tmdbId, string preferredLanguage)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException(nameof(tmdbId));
            }

            var path = GetSeriesDataPath(_configurationManager.ApplicationPaths, tmdbId);

            var filename = string.Format("series-{0}.json", preferredLanguage ?? string.Empty);

            return Path.Combine(path, filename);
        }

        private async Task<RemoteSearchResult> FindByExternalId(string id, string externalSource, CancellationToken cancellationToken)
        {
            var url = string.Format(TmdbUtils.BaseMovieDbUrl + @"3/find/{0}?api_key={1}&external_source={2}",
                id,
                TmdbUtils.ApiKey,
                externalSource);

            using (var response = await TmdbMovieProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = TmdbUtils.AcceptHeader

            }).ConfigureAwait(false))
            {
                using (var json = response.Content)
                {
                    var result = await _jsonSerializer.DeserializeFromStreamAsync<ExternalIdLookupResult>(json).ConfigureAwait(false);

                    if (result != null && result.tv_results != null)
                    {
                        var tv = result.tv_results.FirstOrDefault();

                        if (tv != null)
                        {
                            var tmdbSettings = await TmdbMovieProvider.Current.GetTmdbSettings(cancellationToken).ConfigureAwait(false);
                            var tmdbImageUrl = tmdbSettings.images.GetImageUrl("original");

                            var remoteResult = new RemoteSearchResult
                            {
                                Name = tv.name,
                                SearchProviderName = Name,
                                ImageUrl = string.IsNullOrWhiteSpace(tv.poster_path) ? null : tmdbImageUrl + tv.poster_path
                            };

                            remoteResult.SetProviderId(MetadataProviders.Tmdb, tv.id.ToString(_usCulture));

                            return remoteResult;
                        }
                    }
                }
            }

            return null;
        }

        // After TheTVDB
        public int Order => 1;

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
