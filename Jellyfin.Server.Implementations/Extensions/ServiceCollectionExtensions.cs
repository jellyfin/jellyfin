using System;
using System.IO;
using EFCoreSecondLevelCacheInterceptor;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Extensions;

/// <summary>
/// Extensions for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="IDbContextFactory{TContext}"/> interface to the service collection with second level caching enabled.
    /// </summary>
    /// <param name="serviceCollection">An instance of the <see cref="IServiceCollection"/> interface.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddJellyfinDbContext(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddEFSecondLevelCache(options =>
            options.UseMemoryCacheProvider()
                .CacheAllQueries(CacheExpirationMode.Sliding, TimeSpan.FromMinutes(10))
                .DisableLogging(true)
                .UseCacheKeyPrefix("EF_")
                // Don't cache null values. Remove this optional setting if it's not necessary.
                .SkipCachingResults(result =>
                    result.Value == null || (result.Value is EFTableRows rows && rows.RowsCount == 0)));

        serviceCollection.AddPooledDbContextFactory<JellyfinDb>((serviceProvider, opt) =>
        {
            var applicationPaths = serviceProvider.GetRequiredService<IApplicationPaths>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            opt.UseSqlite($"Filename={Path.Combine(applicationPaths.DataPath, "jellyfin.db")}")
                .AddInterceptors(serviceProvider.GetRequiredService<SecondLevelCacheInterceptor>())
                .UseLoggerFactory(loggerFactory);
        });

        return serviceCollection;
    }
}
