using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Tmdb.Models.TV;
using MediaBrowser.Providers.Tmdb.Movies;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Tmdb.TV
{
    public abstract class TmdbEpisodeProviderBase
    {
        private const string EpisodeUrlPattern = TmdbUtils.BaseTmdbApiUrl + @"3/tv/{0}/season/{1}/episode/{2}?api_key={3}&append_to_response=images,external_ids,credits,videos";
        private readonly IHttpClient _httpClient;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFileSystem _fileSystem;
        private readonly ILocalizationManager _localization;
        private readonly ILogger _logger;

        protected TmdbEpisodeProviderBase(IHttpClient httpClient, IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IFileSystem fileSystem, ILocalizationManager localization, ILoggerFactory loggerFactory)
        {
            _httpClient = httpClient;
            _configurationManager = configurationManager;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _localization = localization;
            _logger = loggerFactory.CreateLogger(GetType().Name);
        }

        protected ILogger Logger => _logger;

        protected async Task<EpisodeResult> GetEpisodeInfo(string seriesTmdbId, int season, int episodeNumber, string preferredMetadataLanguage,
            CancellationToken cancellationToken)
        {
            await EnsureEpisodeInfo(seriesTmdbId, season, episodeNumber, preferredMetadataLanguage, cancellationToken)
                    .ConfigureAwait(false);

            var dataFilePath = GetDataFilePath(seriesTmdbId, season, episodeNumber, preferredMetadataLanguage);

            return _jsonSerializer.DeserializeFromFile<EpisodeResult>(dataFilePath);
        }

        internal Task EnsureEpisodeInfo(string tmdbId, int seasonNumber, int episodeNumber, string language, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tmdbId))
            {
                throw new ArgumentNullException(nameof(tmdbId));
            }
            if (string.IsNullOrEmpty(language))
            {
                throw new ArgumentNullException(nameof(language));
            }

            var path = GetDataFilePath(tmdbId, seasonNumber, episodeNumber, language);

            var fileInfo = _fileSystem.GetFileSystemInfo(path);

            if (fileInfo.Exists)
            {
                // If it's recent or automatic updates are enabled, don't re-download
                if ((DateTime.UtcNow - _fileSystem.GetLastWriteTimeUtc(fileInfo)).TotalDays <= 2)
                {
                    return Task.CompletedTask;
                }
            }

            return DownloadEpisodeInfo(tmdbId, seasonNumber, episodeNumber, language, cancellationToken);
        }

        internal string GetDataFilePath(string tmdbId, int seasonNumber, int episodeNumber, string preferredLanguage)
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

            var filename = string.Format("season-{0}-episode-{1}-{2}.json",
                seasonNumber.ToString(CultureInfo.InvariantCulture),
                episodeNumber.ToString(CultureInfo.InvariantCulture),
                preferredLanguage);

            return Path.Combine(path, filename);
        }

        internal async Task DownloadEpisodeInfo(string id, int seasonNumber, int episodeNumber, string preferredMetadataLanguage, CancellationToken cancellationToken)
        {
            var mainResult = await FetchMainResult(EpisodeUrlPattern, id, seasonNumber, episodeNumber, preferredMetadataLanguage, cancellationToken).ConfigureAwait(false);

            var dataFilePath = GetDataFilePath(id, seasonNumber, episodeNumber, preferredMetadataLanguage);

            Directory.CreateDirectory(Path.GetDirectoryName(dataFilePath));
            _jsonSerializer.SerializeToFile(mainResult, dataFilePath);
        }

        internal async Task<EpisodeResult> FetchMainResult(string urlPattern, string id, int seasonNumber, int episodeNumber, string language, CancellationToken cancellationToken)
        {
            var url = string.Format(urlPattern, id, seasonNumber.ToString(CultureInfo.InvariantCulture), episodeNumber, TmdbUtils.ApiKey);

            if (!string.IsNullOrEmpty(language))
            {
                url += string.Format("&language={0}", language);
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
                    return await _jsonSerializer.DeserializeFromStreamAsync<EpisodeResult>(json).ConfigureAwait(false);
                }
            }
        }

        protected Task<HttpResponseInfo> GetResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClient.GetResponse(new HttpRequestOptions
            {
                CancellationToken = cancellationToken,
                Url = url
            });
        }
    }
}
