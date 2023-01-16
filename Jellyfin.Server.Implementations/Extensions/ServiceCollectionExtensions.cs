using System;
using System.IO;
using EFCoreSecondLevelCacheInterceptor;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

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
                    result.Value is null || (result.Value is EFTableRows rows && rows.RowsCount == 0)));

        serviceCollection.AddPooledDbContextFactory<JellyfinDbContext>((serviceProvider, opt) =>
        {
            var applicationPaths = serviceProvider.GetRequiredService<IApplicationPaths>();
            opt.UseSqlite($"Filename={Path.Combine(applicationPaths.DataPath, "jellyfin.db")}")
                .AddInterceptors(serviceProvider.GetRequiredService<SecondLevelCacheInterceptor>());
        });

        return serviceCollection;
    }
}
