using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Tmdb.Models.TV;
using MediaBrowser.Providers.Tmdb.Movies;
using Microsoft.Extensions.Logging;
using Season = MediaBrowser.Controller.Entities.TV.Season;

namespace MediaBrowser.Providers.Tmdb.TV
{
    public class TmdbSeasonProvider : IRemoteMetadataProvider<Season, SeasonInfo>
    {
        private const string GetTvInfo3 = TmdbUtils.BaseTmdbApiUrl + @"3/tv/{0}/season/{1}?api_key={2}&append_to_response=images,keywords,external_ids,credits,videos";
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;
        private readonly ILogger _logger;

        internal static TmdbSeasonProvider Current { get; private set; }

        public TmdbSeasonProvider(IHttpClient httpClient, IServerConfigurationManager configurationManager, IFileSystem fileSystem, ILocalizationManager localization, IJsonSerializer jsonSerializer, ILoggerFactory loggerFactory)
        {
            _httpClient = httpClient;
            _configurationManager = configurationManager;
            _fileSystem = fileSystem;
            _localization = localization;
            _jsonSerializer = jsonSerializer;
            _logger = loggerFactory.CreateLogger(GetType().Name);
            Current = this;
        }

        public async Task<MetadataResult<Season>> GetMetadata(SeasonInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<Season>();

            info.SeriesProviderIds.TryGetValue(MetadataProviders.Tmdb.ToString(), out string seriesTmdbId);

            var seasonNumber = info.IndexNumber;

            if (!string.IsNullOrWhiteSpace(seriesTmdbId) && seasonNumber.HasValue)
            {
                try
                {
                    var seasonInfo = await GetSeasonInfo(seriesTmdbId, seasonNumber.Value, info.MetadataLanguage, cancellationToken)
                      .ConfigureAwait(false);

                    result.HasMetadata = true;
                    result.Item = new Season();

                    // Don't use moviedb season names for now until if/when we have field-level configuration
                    //result.Item.Name = seasonInfo.name;

                    result.Item.Name = info.Name;

                    result.Item.IndexNumber = seasonNumber;

                    result.Item.Overview = seasonInfo.Overview;

                    if (seasonInfo.External_Ids.Tvdb_Id > 0)
                    {
                        result.Item.SetProviderId(MetadataProviders.Tvdb, seasonInfo.External_Ids.Tvdb_Id.ToString(CultureInfo.InvariantCulture));
                    }

                    var credits = seasonInfo.Credits;
                    if (credits != null)
                    {
                        //Actors, Directors, Writers - all in People
                        //actors come from cast
                        if (credits.Cast != null)
                        {
                            //foreach (var actor in credits.cast.OrderBy(a => a.order)) result.Item.AddPerson(new PersonInfo { Name = actor.name.Trim(), Role = actor.character, Type = PersonType.Actor, SortOrder = actor.order });
                        }

                        //and the rest from crew
                        if (credits.Crew != null)
                        {
                            //foreach (var person in credits.crew) result.Item.AddPerson(new PersonInfo { Name = person.name.Trim(), Role = person.job, Type = person.department });
                        }
                    }

                    result.Item.PremiereDate = seasonInfo.Air_Date;
                    result.Item.ProductionYear = result.Item.PremiereDate.Value.Year;
                }
                catch (HttpException ex)
                {
                    _logger.LogError(ex, "No metadata found for {0}", seasonNumber.Value);

                    if (ex.StatusCode.HasValue && ex.StatusCode.Value == HttpStatusCode.NotFound)
                    {
                        return result;
                    }

                    throw;
                }
            }

            return result;
        }

        public string Name => TmdbUtils.ProviderName;

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(SeasonInfo searchInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<RemoteSearchResult>>(new List<RemoteSearchResult>());
        }

        public Task<HttpResponseInfo> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }

        private async Task<SeasonResult> GetSeasonInfo(string seriesTmdbId, int season, string preferredMetadataLanguage,
            CancellationToken cancellationToken)
        {
            await EnsureSeasonInfo(seriesTmdbId, season, preferredMetadataLanguage, cancellationToken)
                    .ConfigureAwait(false);

            var dataFilePath = GetDataFilePath(seriesTmdbId, season, preferredMetadataLanguage);

            return _jsonSerializer.DeserializeFromFile<SeasonResult>(dataFilePath);
        }

        internal Task EnsureSeasonInfo(string tmdbId, int seasonNumber, string language, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException(nameof(tmdbId));
            }
            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentNullException(nameof(language));
            }

            var path = GetDataFilePath(tmdbId, seasonNumber, language);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 2)
                {
                    return Task.CompletedTask;
                }
            }

            return DownloadSeasonInfo(tmdbId, seasonNumber, language, cancellationToken);
        }

        internal string GetDataFilePath(string tmdbId, int seasonNumber, string preferredLanguage)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException(nameof(tmdbId));
            }
            if (string.IsNullOrEmpty(preferredLanguage))
            {
                throw new ArgumentNullException(nameof(preferredLanguage));
            }

            var path = TmdbSeriesProvider.GetSeriesDataPath(_configurationManager.ApplicationPaths, tmdbId);

            var filename = string.Format("season-{0}-{1}.json",
                seasonNumber.ToString(CultureInfo.InvariantCulture),
                preferredLanguage);

            return Path.Combine(path, filename);
        }

        internal async Task DownloadSeasonInfo(string id, int seasonNumber, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var mainResult = await FetchMainResult(id, seasonNumber, preferredMetadataLanguage, cancellationToken).ConfigureAwait(false);

            var dataFilePath = GetDataFilePath(id, seasonNumber, preferredMetadataLanguage);

            Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath));
            _jsonSerializer.SerializeToFile(mainResult, dataFilePath);
        }

        internal async Task<SeasonResult> FetchMainResult(string id, int seasonNumber, string language, CancellationToken cancellationToken)
        {
            var url = string.Format(GetTvInfo3, id, seasonNumber.ToString(CultureInfo.InvariantCulture), TmdbUtils.ApiKey);

            if (!string.IsNullOrEmpty(language))
            {
                url += string.Format("&language={0}", TmdbMovieProvider.NormalizeLanguage(language));
            }

            var includeImageLanguageParam = TmdbMovieProvider.GetImageLanguagesParam(language);
            // Get images in english and with no language
            url += "&include_image_language=" + includeImageLanguageParam;

            cancellationToken.ThrowIfCancellationRequested();

            using (var response = await TmdbMovieProvider.Current.GetMovieDbResponse(new HttpRequestOptions
            {
                Url = url,
                CancellationToken = cancellationToken,
                AcceptHeader = TmdbUtils.AcceptHeader

            }).ConfigureAwait(false))
            {
                using (var json = response.Content)
                {
                    return await _jsonSerializer.DeserializeFromStreamAsync<SeasonResult>(json).ConfigureAwait(false);
                }
            }
        }
    }
}
