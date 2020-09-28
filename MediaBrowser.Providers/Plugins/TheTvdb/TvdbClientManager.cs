#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        private readonly IMemoryCache _cache;

        /// <summary>
        /// TvDbClients per language.
        /// </summary>
        private readonly ConcurrentDictionary<string, TvDbClientInfo> _tvDbClients = new ConcurrentDictionary<string, TvDbClientInfo>();

        public TvdbClientManager(IMemoryCache memoryCache)
        {
            _cache = memoryCache;
        }

        private async Task<TvDbClient> GetTvDbClient(string language)
        {
            var normalizedLanguage = TvdbUtils.NormalizeLanguage(language) ?? DefaultLanguage;

            var tvDbClientInfo = _tvDbClients.GetOrAdd(normalizedLanguage, key => new TvDbClientInfo(key));

            var tvDbClient = tvDbClientInfo.Client;

            // First time authenticating if the token was never updated or if it's empty in the client
            if (tvDbClientInfo.TokenUpdatedAt == DateTime.MinValue || string.IsNullOrEmpty(tvDbClient.Authentication.Token))
            {
                await tvDbClientInfo.TokenUpdateLock.WaitAsync().ConfigureAwait(false);

                try
                {
                    if (string.IsNullOrEmpty(tvDbClient.Authentication.Token))
                    {
                        await tvDbClient.Authentication.AuthenticateAsync(TvdbUtils.TvdbApiKey).ConfigureAwait(false);
                        tvDbClientInfo.TokenUpdatedAt = DateTime.UtcNow;
                    }
                }
                finally
                {
                    tvDbClientInfo.TokenUpdateLock.Release();
                }
            }

            // Refresh if necessary
            if (tvDbClientInfo.TokenUpdatedAt < DateTime.UtcNow.Subtract(TimeSpan.FromHours(20)))
            {
                await tvDbClientInfo.TokenUpdateLock.WaitAsync().ConfigureAwait(false);

                try
                {
                    if (tvDbClientInfo.TokenUpdatedAt < DateTime.UtcNow.Subtract(TimeSpan.FromHours(20)))
                    {
                        try
                        {
                            await tvDbClient.Authentication.RefreshTokenAsync().ConfigureAwait(false);
                        }
                        catch
                        {
                            await tvDbClient.Authentication.AuthenticateAsync(TvdbUtils.TvdbApiKey).ConfigureAwait(false);
                        }

                        tvDbClientInfo.TokenUpdatedAt = DateTime.UtcNow;
                    }
                }
                finally
                {
                    tvDbClientInfo.TokenUpdateLock.Release();
                }
            }

            return tvDbClient;
        }

        public Task<TvDbResponse<SeriesSearchResult[]>> GetSeriesByNameAsync(string name, string language, CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("series", name, language);
            return TryGetValue(cacheKey, language, tvDbClient => tvDbClient.Search.SearchSeriesByNameAsync(name, cancellationToken));
        }

        public Task<TvDbResponse<Series>> GetSeriesByIdAsync(int tvdbId, string language, CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("series", tvdbId, language);
            return TryGetValue(cacheKey, language, tvDbClient => tvDbClient.Series.GetAsync(tvdbId, cancellationToken));
        }

        public Task<TvDbResponse<EpisodeRecord>> GetEpisodesAsync(int episodeTvdbId, string language, CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("episode", episodeTvdbId, language);
            return TryGetValue(cacheKey, language, tvDbClient => tvDbClient.Episodes.GetAsync(episodeTvdbId, cancellationToken));
        }

        public async Task<List<EpisodeRecord>> GetAllEpisodesAsync(int tvdbId, string language, CancellationToken cancellationToken)
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
            return TryGetValue(cacheKey, language, tvDbClient => tvDbClient.Search.SearchSeriesByImdbIdAsync(imdbId, cancellationToken));
        }

        public Task<TvDbResponse<SeriesSearchResult[]>> GetSeriesByZap2ItIdAsync(
            string zap2ItId,
            string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("series", zap2ItId, language);
            return TryGetValue(cacheKey, language, tvDbClient => tvDbClient.Search.SearchSeriesByZap2ItIdAsync(zap2ItId, cancellationToken));
        }

        public Task<TvDbResponse<Actor[]>> GetActorsAsync(
            int tvdbId,
            string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("actors", tvdbId, language);
            return TryGetValue(cacheKey, language, tvDbClient => tvDbClient.Series.GetActorsAsync(tvdbId, cancellationToken));
        }

        public Task<TvDbResponse<Image[]>> GetImagesAsync(
            int tvdbId,
            ImagesQuery imageQuery,
            string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("images", tvdbId, language, imageQuery);
            return TryGetValue(cacheKey, language, tvDbClient => tvDbClient.Series.GetImagesAsync(tvdbId, imageQuery, cancellationToken));
        }

        public Task<TvDbResponse<Language[]>> GetLanguagesAsync(CancellationToken cancellationToken)
        {
            return TryGetValue("languages", null, tvDbClient => tvDbClient.Languages.GetAllAsync(cancellationToken));
        }

        public Task<TvDbResponse<EpisodesSummary>> GetSeriesEpisodeSummaryAsync(
            int tvdbId,
            string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey("seriesepisodesummary", tvdbId, language);
            return TryGetValue(cacheKey, language, tvDbClient => tvDbClient.Series.GetEpisodesSummaryAsync(tvdbId, cancellationToken));
        }

        public Task<TvDbResponse<EpisodeRecord[]>> GetEpisodesPageAsync(
            int tvdbId,
            int page,
            EpisodeQuery episodeQuery,
            string language,
            CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey(language, tvdbId, episodeQuery);
            return TryGetValue(cacheKey, language, tvDbClient => tvDbClient.Series.GetEpisodesAsync(tvdbId, page, episodeQuery, cancellationToken));
        }

        public Task<string> GetEpisodeTvdbId(
            EpisodeInfo searchInfo,
            string language,
            CancellationToken cancellationToken)
        {
            searchInfo.SeriesProviderIds.TryGetValue(nameof(MetadataProvider.Tvdb),
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
                        // aired order
                        episodeQuery.AiredEpisode = searchInfo.IndexNumber.Value;
                        episodeQuery.AiredSeason = searchInfo.ParentIndexNumber.Value;
                        break;
                }
            }
            else if (searchInfo.PremiereDate.HasValue)
            {
                // tvdb expects yyyy-mm-dd format
                episodeQuery.FirstAired = searchInfo.PremiereDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            return GetEpisodeTvdbId(Convert.ToInt32(seriesTvdbId, CultureInfo.InvariantCulture), episodeQuery, language, cancellationToken);
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
            return episodePage.Data.FirstOrDefault()?.Id.ToString(CultureInfo.InvariantCulture);
        }

        public Task<TvDbResponse<EpisodeRecord[]>> GetEpisodesPageAsync(
            int tvdbId,
            EpisodeQuery episodeQuery,
            string language,
            CancellationToken cancellationToken)
        {
            return GetEpisodesPageAsync(tvdbId, 1, episodeQuery, language, cancellationToken);
        }

        public async IAsyncEnumerable<KeyType> GetImageKeyTypesForSeriesAsync(int tvdbId, string language, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey(nameof(TvDbClient.Series.GetImagesSummaryAsync), tvdbId);
            var imagesSummary = await TryGetValue(cacheKey, language, tvDbClient => tvDbClient.Series.GetImagesSummaryAsync(tvdbId, cancellationToken)).ConfigureAwait(false);

            if (imagesSummary.Data.Fanart > 0)
            {
                yield return KeyType.Fanart;
            }

            if (imagesSummary.Data.Series > 0)
            {
                yield return KeyType.Series;
            }

            if (imagesSummary.Data.Poster > 0)
            {
                yield return KeyType.Poster;
            }
        }

        public async IAsyncEnumerable<KeyType> GetImageKeyTypesForSeasonAsync(int tvdbId, string language, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey(nameof(TvDbClient.Series.GetImagesSummaryAsync), tvdbId);
            var imagesSummary = await TryGetValue(cacheKey, language, tvDbClient => tvDbClient.Series.GetImagesSummaryAsync(tvdbId, cancellationToken)).ConfigureAwait(false);

            if (imagesSummary.Data.Season > 0)
            {
                yield return KeyType.Season;
            }

            if (imagesSummary.Data.Fanart > 0)
            {
                yield return KeyType.Fanart;
            }

            // TODO seasonwide is not supported in TvDbSharper
        }

        private Task<T> TryGetValue<T>(string key, string language, Func<TvDbClient, Task<T>> resultFactory)
        {
            return _cache.GetOrCreateAsync(key, async entry =>
             {
                 entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                 var tvDbClient = await GetTvDbClient(language).ConfigureAwait(false);

                 var result = await resultFactory.Invoke(tvDbClient).ConfigureAwait(false);

                 return result;
             });
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

        private class TvDbClientInfo
        {
            public TvDbClientInfo(string language)
            {
                Client = new TvDbClient()
                {
                    AcceptedLanguage = language
                };

                TokenUpdateLock = new SemaphoreSlim(1, 1);
                TokenUpdatedAt = DateTime.MinValue;
            }

            public TvDbClient Client { get; }

            public SemaphoreSlim TokenUpdateLock { get; }

            public DateTime TokenUpdatedAt { get; set; }
        }
    }
}
