using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Caching.Memory;
using TvDbSharper;
using TvDbSharper.Dto;

namespace MediaBrowser.Providers.TV
{
    // TODO add to DI once Bond's PR is merged
    public sealed class TvDbClientManager
    {
        private static volatile TvDbClientManager instance;
        // TODO add to DI once Bond's PR is merged
        private readonly SemaphoreSlim _cacheWriteLock = new SemaphoreSlim(1, 1);
        private static MemoryCache _cache;
        private static readonly object syncRoot = new object();
        private static TvDbClient tvDbClient;
        private static DateTime tokenCreatedAt;

        private TvDbClientManager()
        {
            tvDbClient = new TvDbClient();
            tvDbClient.Authentication.AuthenticateAsync(TVUtils.TvdbApiKey);
            tokenCreatedAt = DateTime.Now;
        }

        public static TvDbClientManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (syncRoot)
                {
                    if (instance == null)
                    {
                        instance = new TvDbClientManager();
                        _cache = new MemoryCache(new MemoryCacheOptions());
                    }
                }

                return instance;
            }
        }

        public TvDbClient TvDbClient
        {
            get
            {
                // Refresh if necessary
                if (tokenCreatedAt > DateTime.Now.Subtract(TimeSpan.FromHours(20)))
                {
                    try
                    {
                        tvDbClient.Authentication.RefreshTokenAsync();
                    }
                    catch
                    {
                        tvDbClient.Authentication.AuthenticateAsync(TVUtils.TvdbApiKey);
                    }

                    tokenCreatedAt = DateTime.Now;
                }
                // Default to English
                tvDbClient.AcceptedLanguage = "en";
                return tvDbClient;
            }
        }

        public Task<TvDbResponse<SeriesSearchResult[]>> GetSeriesByNameAsync(string name, CancellationToken cancellationToken)
        {
            return TryGetValue("series" + name,() => TvDbClient.Search.SearchSeriesByNameAsync(name, cancellationToken));
        }

        public Task<TvDbResponse<Series>> GetSeriesByIdAsync(int tvdbId, CancellationToken cancellationToken)
        {
            return TryGetValue("series" + tvdbId,() => TvDbClient.Series.GetAsync(tvdbId, cancellationToken));
        }

        public Task<TvDbResponse<EpisodeRecord>> GetEpisodesAsync(int episodeTvdbId, CancellationToken cancellationToken)
        {
            return TryGetValue("episode" + episodeTvdbId,() => TvDbClient.Episodes.GetAsync(episodeTvdbId, cancellationToken));
        }

        public async Task<List<EpisodeRecord>> GetAllEpisodesAsync(int tvdbId, CancellationToken cancellationToken)
        {
            // Traverse all episode pages and join them together
            var episodes = new List<EpisodeRecord>();
            var episodePage = await GetEpisodesPageAsync(tvdbId, new EpisodeQuery(), cancellationToken);
            episodes.AddRange(episodePage.Data);
            if (!episodePage.Links.Next.HasValue || !episodePage.Links.Last.HasValue)
            {
                return episodes;
            }

            int next = episodePage.Links.Next.Value;
            int last = episodePage.Links.Last.Value;

            for (var page = next; page <= last; ++page)
            {
                episodePage = await GetEpisodesPageAsync(tvdbId, page, new EpisodeQuery(), cancellationToken);
                episodes.AddRange(episodePage.Data);
            }

            return episodes;
        }

        public Task<TvDbResponse<SeriesSearchResult[]>> GetSeriesByImdbIdAsync(string imdbId, CancellationToken cancellationToken)
        {
            return TryGetValue("series" + imdbId,() => TvDbClient.Search.SearchSeriesByImdbIdAsync(imdbId, cancellationToken));
        }

        public Task<TvDbResponse<SeriesSearchResult[]>> GetSeriesByZap2ItIdAsync(string zap2ItId, CancellationToken cancellationToken)
        {
            return TryGetValue("series" + zap2ItId,() => TvDbClient.Search.SearchSeriesByZap2ItIdAsync(zap2ItId, cancellationToken));
        }
        public Task<TvDbResponse<Actor[]>> GetActorsAsync(int tvdbId, CancellationToken cancellationToken)
        {
            return TryGetValue("actors" + tvdbId,() => TvDbClient.Series.GetActorsAsync(tvdbId, cancellationToken));
        }

        public Task<TvDbResponse<Image[]>> GetImagesAsync(int tvdbId, ImagesQuery imageQuery, CancellationToken cancellationToken)
        {
            var cacheKey = "images" + tvdbId + "keytype" + imageQuery.KeyType + "subkey" + imageQuery.SubKey;
            return TryGetValue(cacheKey,() => TvDbClient.Series.GetImagesAsync(tvdbId, imageQuery, cancellationToken));
        }

        public Task<TvDbResponse<Language[]>> GetLanguagesAsync(CancellationToken cancellationToken)
        {
            return TryGetValue("languages",() => TvDbClient.Languages.GetAllAsync(cancellationToken));
        }

        public Task<TvDbResponse<EpisodesSummary>> GetSeriesEpisodeSummaryAsync(int tvdbId, CancellationToken cancellationToken)
        {
            return TryGetValue("seriesepisodesummary" + tvdbId,
                () => TvDbClient.Series.GetEpisodesSummaryAsync(tvdbId, cancellationToken));
        }

        public Task<TvDbResponse<EpisodeRecord[]>> GetEpisodesPageAsync(int tvdbId, int page, EpisodeQuery episodeQuery, CancellationToken cancellationToken)
        {
            // Not quite as dynamic as it could be
            var cacheKey = "episodespage" + tvdbId + "page" + page;
            if (episodeQuery.AiredSeason.HasValue)
            {
                cacheKey += "airedseason" + episodeQuery.AiredSeason.Value;
            }
            if (episodeQuery.AiredEpisode.HasValue)
            {
                cacheKey += "airedepisode" + episodeQuery.AiredEpisode.Value;
            }
            return TryGetValue(cacheKey,
                () => TvDbClient.Series.GetEpisodesAsync(tvdbId, page, episodeQuery, cancellationToken));
        }

        public Task<string> GetEpisodeTvdbId(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            searchInfo.SeriesProviderIds.TryGetValue(MetadataProviders.Tvdb.ToString(),
                out var seriesTvdbId);

            var episodeQuery = new EpisodeQuery();

            // Prefer SxE over premiere date as it is more robust
            if (searchInfo.IndexNumber.HasValue && searchInfo.ParentIndexNumber.HasValue)
            {
                episodeQuery.AiredEpisode = searchInfo.IndexNumber.Value;
                episodeQuery.AiredSeason = searchInfo.ParentIndexNumber.Value;
            }
            else if (searchInfo.PremiereDate.HasValue)
            {
                // tvdb expects yyyy-mm-dd format
                episodeQuery.FirstAired = searchInfo.PremiereDate.Value.ToString("yyyy-MM-dd");
            }

            return GetEpisodeTvdbId(Convert.ToInt32(seriesTvdbId), episodeQuery, cancellationToken);
        }

        public async Task<string> GetEpisodeTvdbId(int seriesTvdbId, EpisodeQuery episodeQuery, CancellationToken cancellationToken)
        {
            var episodePage = await GetEpisodesPageAsync(Convert.ToInt32(seriesTvdbId), episodeQuery, cancellationToken);
            return episodePage.Data.FirstOrDefault()?.Id.ToString();
        }

        public Task<TvDbResponse<EpisodeRecord[]>> GetEpisodesPageAsync(int tvdbId, EpisodeQuery episodeQuery, CancellationToken cancellationToken)
        {
            return GetEpisodesPageAsync(tvdbId, 1, episodeQuery, cancellationToken);
        }

        private async Task<T> TryGetValue<T>(object key, Func<Task<T>> resultFactory)
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

                var result = await resultFactory.Invoke();
                _cache.Set(key, result, TimeSpan.FromHours(1));
                return result;
            }
            finally
            {
                _cacheWriteLock.Release();
            }
        }
    }
}
