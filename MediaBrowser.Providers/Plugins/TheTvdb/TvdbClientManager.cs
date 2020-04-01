using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Caching.Memory;
using TvDbSharper;
using TvDbSharper.Dto;

namespace MediaBrowser.Providers.Plugins.TheTvdb
{
    public class TvdbClientManager
    {
        private const string DefaultLanguage = "en";

        private readonly SemaphoreSlim _cacheWriteLock = new SemaphoreSlim(1, 1);
        private readonly IMemoryCache _cache;
        private readonly TvDbClient _tvDbClient;
        private DateTime _tokenCreatedAt;

        public TvdbClientManager(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
            _tvDbClient = new TvDbClient();
        }

        private TvDbClient TvDbClient
        {
            get
            {
                if (string.IsNullOrEmpty(_tvDbClient.Authentication.Token))
                {
                    _tvDbClient.Authentication.AuthenticateAsync(TvdbUtils.TvdbApiKey).GetAwaiter().GetResult();
                    _tokenCreatedAt = DateTime.Now;
                }

                // Refresh if necessary
                if (_tokenCreatedAt < DateTime.Now.Subtract(TimeSpan.FromHours(20)))
                {
                    try
                    {
                        _tvDbClient.Authentication.RefreshTokenAsync().GetAwaiter().GetResult();
                    }
                    catch
                    {
                        _tvDbClient.Authentication.AuthenticateAsync(TvdbUtils.TvdbApiKey).GetAwaiter().GetResult();
                    }

                    _tokenCreatedAt = DateTime.Now;
                }

                return _tvDbClient;
            }
        }

        public Task<TvDbResponse<SeriesSearchResult[]>> GetSeriesByNameAsync(string name, string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("series", name, language);
            return TryGetValue(cacheKey, language, () => TvDbClient.Search.SearchSeriesByNameAsync(name, cancellationToken));
        }

        public Task<TvDbResponse<Series>> GetSeriesByIdAsync(int tvdbId, string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("series", tvdbId, language);
            return TryGetValue(cacheKey, language, () => TvDbClient.Series.GetAsync(tvdbId, cancellationToken));
        }

        public Task<TvDbResponse<EpisodeRecord>> GetEpisodesAsync(int episodeTvdbId, string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("episode", episodeTvdbId, language);
            return TryGetValue(cacheKey, language, () => TvDbClient.Episodes.GetAsync(episodeTvdbId, cancellationToken));
        }

        public async Task<List<EpisodeRecord>> GetAllEpisodesAsync(int tvdbId, string language,
            CancellationToken cancellationToken)
        {
            // Traverse all episode pages and join them together
            var episodes = new List<EpisodeRecord>();
            var episodePage = await GetEpisodesPageAsync(tvdbId, new EpisodeQuery(), language, cancellationToken)
                .ConfigureAwait(false);
            episodes.AddRange(episodePage.Data);
            if (!episodePage.Links.Next.HasValue || !episodePage.Links.Last.HasValue)
            {
                return episodes;
            }

            int next = episodePage.Links.Next.Value;
            int last = episodePage.Links.Last.Value;

            for (var page = next; page <= last; ++page)
            {
                episodePage = await GetEpisodesPageAsync(tvdbId, page, new EpisodeQuery(), language, cancellationToken)
                    .ConfigureAwait(false);
                episodes.AddRange(episodePage.Data);
            }

            return episodes;
        }

        public Task<TvDbResponse<SeriesSearchResult[]>> GetSeriesByImdbIdAsync(
            string imdbId,
            string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("series", imdbId, language);
            return TryGetValue(cacheKey, language, () => TvDbClient.Search.SearchSeriesByImdbIdAsync(imdbId, cancellationToken));
        }

        public Task<TvDbResponse<SeriesSearchResult[]>> GetSeriesByZap2ItIdAsync(
            string zap2ItId,
            string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("series", zap2ItId, language);
            return TryGetValue(cacheKey, language, () => TvDbClient.Search.SearchSeriesByZap2ItIdAsync(zap2ItId, cancellationToken));
        }
        public Task<TvDbResponse<Actor[]>> GetActorsAsync(
            int tvdbId,
            string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("actors", tvdbId, language);
            return TryGetValue(cacheKey, language, () => TvDbClient.Series.GetActorsAsync(tvdbId, cancellationToken));
        }

        public Task<TvDbResponse<Image[]>> GetImagesAsync(
            int tvdbId,
            ImagesQuery imageQuery,
            string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("images", tvdbId, language, imageQuery);
            return TryGetValue(cacheKey, language, () => TvDbClient.Series.GetImagesAsync(tvdbId, imageQuery, cancellationToken));
        }

        public Task<TvDbResponse<Language[]>> GetLanguagesAsync(CancellationToken cancellationToken)
        {
            return TryGetValue("languages", null, () => TvDbClient.Languages.GetAllAsync(cancellationToken));
        }

        public Task<TvDbResponse<EpisodesSummary>> GetSeriesEpisodeSummaryAsync(
            int tvdbId,
            string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("seriesepisodesummary", tvdbId, language);
            return TryGetValue(cacheKey, language,
                () => TvDbClient.Series.GetEpisodesSummaryAsync(tvdbId, cancellationToken));
        }

        public Task<TvDbResponse<EpisodeRecord[]>> GetEpisodesPageAsync(
            int tvdbId,
            int page,
            EpisodeQuery episodeQuery,
            string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey(language, tvdbId, episodeQuery);

            return TryGetValue(cacheKey, language,
                () => TvDbClient.Series.GetEpisodesAsync(tvdbId, page, episodeQuery, cancellationToken));
        }

        public Task<string> GetEpisodeTvdbId(
            EpisodeInfo searchInfo,
            string language,
            CancellationToken cancellationToken)
        {
            searchInfo.SeriesProviderIds.TryGetValue(MetadataProviders.Tvdb.ToString(),
                out var seriesTvdbId);

            var episodeQuery = new EpisodeQuery();

            // Prefer SxE over premiere date as it is more robust
            if (searchInfo.IndexNumber.HasValue && searchInfo.ParentIndexNumber.HasValue)
            {
                switch (searchInfo.SeriesDisplayOrder)
                {
                    case "dvd":
                        episodeQuery.DvdEpisode = searchInfo.IndexNumber.Value;
                        episodeQuery.DvdSeason = searchInfo.ParentIndexNumber.Value;
                        break;
                    case "absolute":
                        episodeQuery.AbsoluteNumber = searchInfo.IndexNumber.Value;
                        break;
                    default:
                        //aired order
                        episodeQuery.AiredEpisode = searchInfo.IndexNumber.Value;
                        episodeQuery.AiredSeason = searchInfo.ParentIndexNumber.Value;
                        break;
                }
            }
            else if (searchInfo.PremiereDate.HasValue)
            {
                // tvdb expects yyyy-mm-dd format
                episodeQuery.FirstAired = searchInfo.PremiereDate.Value.ToString("yyyy-MM-dd");
            }

            return GetEpisodeTvdbId(Convert.ToInt32(seriesTvdbId), episodeQuery, language, cancellationToken);
        }

        public async Task<string> GetEpisodeTvdbId(
            int seriesTvdbId,
            EpisodeQuery episodeQuery,
            string language,
            CancellationToken cancellationToken)
        {
            var episodePage =
                await GetEpisodesPageAsync(Convert.ToInt32(seriesTvdbId), episodeQuery, language, cancellationToken)
                    .ConfigureAwait(false);
            return episodePage.Data.FirstOrDefault()?.Id.ToString();
        }

        public Task<TvDbResponse<EpisodeRecord[]>> GetEpisodesPageAsync(
            int tvdbId,
            EpisodeQuery episodeQuery,
            string language,
            CancellationToken cancellationToken)
        {
            return GetEpisodesPageAsync(tvdbId, 1, episodeQuery, language, cancellationToken);
        }

        private async Task<T> TryGetValue<T>(string key, string language, Func<Task<T>> resultFactory)
        {
            if (_cache.TryGetValue(key, out T cachedValue))
            {
                return cachedValue;
            }

            await _cacheWriteLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_cache.TryGetValue(key, out cachedValue))
                {
                    return cachedValue;
                }

                _tvDbClient.AcceptedLanguage = TvdbUtils.NormalizeLanguage(language) ?? DefaultLanguage;
                var result = await resultFactory.Invoke().ConfigureAwait(false);
                _cache.Set(key, result, TimeSpan.FromHours(1));
                return result;
            }
            finally
            {
                _cacheWriteLock.Release();
            }
        }

        private static string GenerateKey(params object[] objects)
        {
            var key = string.Empty;

            foreach (var obj in objects)
            {
                var objType = obj.GetType();
                if (objType.IsPrimitive || objType == typeof(string))
                {
                    key += obj + ";";
                }
                else
                {
                    foreach (PropertyInfo propertyInfo in objType.GetProperties())
                    {
                        var currentValue = propertyInfo.GetValue(obj, null);
                        if (currentValue == null)
                        {
                            continue;
                        }

                        key += propertyInfo.Name + "=" + currentValue + ";";
                    }
                }
            }

            return key;
        }
    }
}
