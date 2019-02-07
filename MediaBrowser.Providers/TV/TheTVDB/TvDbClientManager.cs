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

        public async Task<SeriesSearchResult[]> GetSeriesByName(string name, CancellationToken cancellationToken)
        {
            return await TryGetValue(name,
                async () => (await TvDbClient.Search.SearchSeriesByNameAsync(name, cancellationToken)).Data);
        }

        public async Task<Series> GetSeriesById(int tvdbId, CancellationToken cancellationToken)
        {
            return await TryGetValue(tvdbId,
                async () => (await TvDbClient.Series.GetAsync(tvdbId, cancellationToken)).Data);
        }

        private async Task<T> TryGetValue<T>(object key, Func<Task<T>> resultFactory)
        {
            if (_cache.TryGetValue(key, out T cachedValue))
            {
                return cachedValue;
            }
            using (_cacheWriteLock)
            {
                if (_cache.TryGetValue(key, out cachedValue))
                {
                    return cachedValue;
                }
                var result = await resultFactory.Invoke();
                _cache.Set(key, result, DateTimeOffset.UtcNow.AddHours(1));
                return result;
            }
        }
    }
}
