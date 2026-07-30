#pragma warning disable CS1591

using System;
using Jellyfin.Data.Events.Users;
using MediaBrowser.Controller.CustomNetflix;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.MediaSegments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Jellyfin.Server.Implementations.CustomNetflix;

public static class CustomNetflixServiceCollectionExtensions
{
    public static IServiceCollection AddCustomNetflixServices(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("CustomNetflix").Get<CustomNetflixOptions>()
            ?? new CustomNetflixOptions();
        options.PostgreSqlConnectionString ??= configuration["CustomNetflix:PostgresConnectionString"]
            ?? configuration.GetConnectionString("CustomNetflixPostgres");
        options.RedisConnectionString ??= configuration.GetConnectionString("CustomNetflixRedis")
            ?? configuration["Redis:ConnectionString"];
        options.MaxProfilesPerAccount = Math.Clamp(options.MaxProfilesPerAccount, 1, 20);

        services.AddSingleton(Options.Create(options));

        if (options.IsPostgreSqlEnabled)
        {
            try
            {
                services.AddSingleton(NpgsqlDataSource.Create(options.PostgreSqlConnectionString!));
                services.AddSingleton<ICustomNetflixRepository, PostgreSqlCustomNetflixRepository>();
            }
            catch (ArgumentException)
            {
                services.AddSingleton<ICustomNetflixRepository>(
                    new DisabledCustomNetflixRepository(
                        isConfigured: true,
                        failureMessage: "CustomNetflix PostgreSQL connection string is invalid."));
            }
        }
        else
        {
            services.AddSingleton<ICustomNetflixRepository, DisabledCustomNetflixRepository>();
        }

        if (options.IsRedisEnabled)
        {
            services.AddSingleton<ICustomNetflixCacheService, RedisCustomNetflixCacheService>();
        }
        else
        {
            services.AddSingleton<ICustomNetflixCacheService, NullCustomNetflixCacheService>();
        }

        services.AddScoped<ICustomNetflixProfileService, CustomNetflixProfileService>();
        services.AddScoped<CustomNetflixCardDtoCache>();
        services.AddScoped<ICustomNetflixActiveProfileService, CustomNetflixActiveProfileService>();
        services.AddScoped<IEventConsumer<UserDeletedEventArgs>, CustomNetflixUserDeletedConsumer>();
        services.AddScoped<ICustomNetflixNativePlaystateSyncService, CustomNetflixNativePlaystateSyncService>();
        services.AddScoped<ICustomNetflixWatchProgressService, CustomNetflixWatchProgressService>();
        services.AddScoped<ICustomNetflixWatchHistoryService, CustomNetflixWatchHistoryService>();
        services.AddScoped<ICustomNetflixMyListService, CustomNetflixMyListService>();
        services.AddScoped<ICustomNetflixRecommendationService, CustomNetflixRecommendationService>();
        services.AddScoped<ICustomNetflixFeedbackService, CustomNetflixFeedbackService>();
        services.AddScoped<ICustomNetflixHomeRowsService, CustomNetflixHomeRowsService>();
        services.AddScoped<ICustomNetflixItemDetailsService, CustomNetflixItemDetailsService>();
        services.AddScoped<ICustomNetflixAutoplayService, CustomNetflixAutoplayService>();
        services.AddScoped<ICustomNetflixSegmentService, CustomNetflixSegmentService>();
        services.AddSingleton<IMediaSegmentProvider, CustomNetflixManualMediaSegmentProvider>();
        services.AddScoped<ICustomNetflixRankingService, CustomNetflixRankingService>();
        services.AddSingleton<CustomNetflixWatchProgressBuffer>();
        services.AddSingleton<ICustomNetflixWatchProgressBuffer>(provider => provider.GetRequiredService<CustomNetflixWatchProgressBuffer>());
        services.AddSingleton<CustomNetflixWatchEventBuffer>();
        services.AddSingleton<ICustomNetflixWatchEventBuffer>(provider => provider.GetRequiredService<CustomNetflixWatchEventBuffer>());
        services.AddSingleton<CustomNetflixSchemaState>();
        services.AddHealthChecks()
            .AddCheck<CustomNetflixHealthCheck>("CustomNetflixPostgreSQL", tags: ["ready"])
            .AddCheck<CustomNetflixRedisHealthCheck>("CustomNetflixRedis", tags: ["optional"]);
        services.AddHostedService<CustomNetflixSchemaInitializer>();
        services.AddHostedService(provider => provider.GetRequiredService<CustomNetflixWatchProgressBuffer>());
        services.AddHostedService(provider => provider.GetRequiredService<CustomNetflixWatchEventBuffer>());
        services.AddHostedService<CustomNetflixRankingSnapshotWorker>();

        return services;
    }
}
