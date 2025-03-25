using System;
using System.Collections.Generic;
using System.Reflection;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using JellyfinDbProviderFactory = System.Func<System.IServiceProvider, Jellyfin.Database.Implementations.IJellyfinDatabaseProvider>;

namespace Jellyfin.Server.Implementations.Extensions;

/// <summary>
/// Extensions for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static IEnumerable<Type> DatabaseProviderTypes()
    {
        yield return typeof(SqliteDatabaseProvider);
    }

    private static IDictionary<string, JellyfinDbProviderFactory> GetSupportedDbProviders()
    {
        var items = new Dictionary<string, JellyfinDbProviderFactory>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var providerType in DatabaseProviderTypes())
        {
            var keyAttribute = providerType.GetCustomAttribute<JellyfinDatabaseProviderKeyAttribute>();
            if (keyAttribute is null || string.IsNullOrWhiteSpace(keyAttribute.DatabaseProviderKey))
            {
                continue;
            }

            var provider = providerType;
            items[keyAttribute.DatabaseProviderKey] = (services) => (IJellyfinDatabaseProvider)ActivatorUtilities.CreateInstance(services, providerType);
        }

        return items;
    }

    /// <summary>
    /// Adds the <see cref="IDbContextFactory{TContext}"/> interface to the service collection with second level caching enabled.
    /// </summary>
    /// <param name="serviceCollection">An instance of the <see cref="IServiceCollection"/> interface.</param>
    /// <param name="configurationManager">The server configuration manager.</param>
    /// <param name="configuration">The startup Configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddJellyfinDbContext(
        this IServiceCollection serviceCollection,
        IServerConfigurationManager configurationManager,
        IConfiguration configuration)
    {
        var efCoreConfiguration = configurationManager.GetConfiguration<DatabaseConfigurationOptions>("database");
        var providers = GetSupportedDbProviders();
        JellyfinDbProviderFactory? providerFactory = null;

        if (efCoreConfiguration?.DatabaseType is null)
        {
            var cmdMigrationArgument = configuration.GetValue<string>("migration-provider");
            if (!string.IsNullOrWhiteSpace(cmdMigrationArgument))
            {
                efCoreConfiguration = new DatabaseConfigurationOptions()
                {
                    DatabaseType = cmdMigrationArgument,
                };
            }
            else
            {
                // when nothing is setup via new Database configuration, fallback to SQLite with default settings.
                efCoreConfiguration = new DatabaseConfigurationOptions()
                {
                    DatabaseType = "Jellyfin-SQLite",
                };
                configurationManager.SaveConfiguration("database", efCoreConfiguration);
            }
        }

        if (!providers.TryGetValue(efCoreConfiguration.DatabaseType.ToUpperInvariant(), out providerFactory!))
        {
            throw new InvalidOperationException($"Jellyfin cannot find the database provider of type '{efCoreConfiguration.DatabaseType}'. Supported types are {string.Join(", ", providers.Keys)}");
        }

        serviceCollection.AddSingleton<IJellyfinDatabaseProvider>(providerFactory!);

        serviceCollection.AddPooledDbContextFactory<JellyfinDbContext>((serviceProvider, opt) =>
        {
            var provider = serviceProvider.GetRequiredService<IJellyfinDatabaseProvider>();
            provider.Initialise(opt);
        });

        return serviceCollection;
    }
}
