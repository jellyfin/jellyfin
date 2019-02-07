using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Internal;
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
            return TryGetValue(name,() => TvDbClient.Search.SearchSeriesByNameAsync(name, cancellationToken));
        }

        public Task<TvDbResponse<Series>> GetSeriesByIdAsync(int tvdbId, CancellationToken cancellationToken)
        {
            return TryGetValue(tvdbId,() => TvDbClient.Series.GetAsync(tvdbId, cancellationToken));
        }

        public Task<TvDbResponse<EpisodeRecord>> GetEpisodesAsync(int tvdbId, CancellationToken cancellationToken)
        {
            return TryGetValue(tvdbId,() => TvDbClient.Episodes.GetAsync(tvdbId, cancellationToken));
        }

        public Task<TvDbResponse<SeriesSearchResult[]>> GetSeriesByImdbIdAsync(string imdbId, CancellationToken cancellationToken)
        {
            return TryGetValue(imdbId,() => TvDbClient.Search.SearchSeriesByImdbIdAsync(imdbId, cancellationToken));
        }

        public Task<TvDbResponse<SeriesSearchResult[]>> GetSeriesByZap2ItIdAsync(string zap2ItId, CancellationToken cancellationToken)
        {
            return TryGetValue(zap2ItId,() => TvDbClient.Search.SearchSeriesByImdbIdAsync(zap2ItId, cancellationToken));
        }
        public Task<TvDbResponse<Actor[]>> GetActorsAsync(int tvdbId, CancellationToken cancellationToken)
        {
            return TryGetValue(tvdbId,() => TvDbClient.Series.GetActorsAsync(tvdbId, cancellationToken));
        }

        public Task<TvDbResponse<Image[]>> GetImagesAsync(int tvdbId, ImagesQuery imageQuery, CancellationToken cancellationToken)
        {
            return TryGetValue(tvdbId,() => TvDbClient.Series.GetImagesAsync(tvdbId, imageQuery, cancellationToken));
        }

        private async Task<T> TryGetValue<T>(object key, Func<Task<T>> resultFactory)
        {
            if (_cache.TryGetValue(key, out T cachedValue))
            {
                Console.WriteLine("Cache hit!!!");
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
