using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    private static JellyfinDbProviderFactory? LoadDatabasePlugin(CustomDatabaseOptions customProviderOptions, IApplicationPaths applicationPaths)
    {
        var plugin = Directory.EnumerateDirectories(applicationPaths.PluginsPath)
            .Where(e => Path.GetFileName(e)!.StartsWith(customProviderOptions.PluginName, StringComparison.OrdinalIgnoreCase))
            .Order()
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"The requested custom database plugin with the name '{customProviderOptions.PluginName}' could not been found in '{applicationPaths.PluginsPath}'");

        var dbProviderAssembly = Path.Combine(plugin, Path.ChangeExtension(customProviderOptions.PluginAssembly, "dll"));
        if (!File.Exists(dbProviderAssembly))
        {
            throw new InvalidOperationException($"Could not find the requested assembly at '{dbProviderAssembly}'");
        }

        // we have to load the assembly without proxy to ensure maximum performance for this.
        var assembly = Assembly.LoadFrom(dbProviderAssembly);
        var dbProviderType = assembly.GetExportedTypes().FirstOrDefault(f => f.IsAssignableTo(typeof(IJellyfinDatabaseProvider)))
            ?? throw new InvalidOperationException($"Could not find any type implementing the '{nameof(IJellyfinDatabaseProvider)}' interface.");

        return (services) => (IJellyfinDatabaseProvider)ActivatorUtilities.CreateInstance(services, dbProviderType);
    }

    /// <summary>
    /// Adds the <see cref="IDbContextFactory{TContext}"/> interface to the service collection with second level caching enabled.
    /// </summary>
    /// <param name="serviceCollection">An instance of the <see cref="IServiceCollection"/> interface.</param>
    /// <param name="configuration">The Configuration Root.</param>
    /// <param name="appPaths">Server app paths.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddJellyfinDbContext(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        IServerApplicationPaths appPaths)
    {
        serviceCollection.AddSingleton<DatabaseConfigurationOptions>(sp =>
        {
            var dbConfig = sp.GetRequiredService<IWritableOptions<DatabaseConfigurationOptions>>();
            var efCoreConfiguration = dbConfig.Value;

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
                    dbConfig.Update(config =>
                    {
                        config.DatabaseType = "Jellyfin-SQLite";
                        config.LockingBehavior = DatabaseLockingBehaviorTypes.NoLock;
                        efCoreConfiguration = config;
                    });
                    // when nothing is setup via new Database configuration, fallback to SQLite with default settings.
                }
            }

            return efCoreConfiguration!;
        });

        serviceCollection.AddSingleton<IJellyfinDatabaseProvider>(sp =>
        {
            var efCoreConfiguration = sp.GetRequiredService<DatabaseConfigurationOptions>();
            JellyfinDbProviderFactory? providerFactory = null;

            if (efCoreConfiguration.DatabaseType?.Equals("PLUGIN_PROVIDER", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (efCoreConfiguration.CustomProviderOptions is null)
                {
                    throw new InvalidOperationException("The custom database provider must declare the custom provider options to work");
                }

                providerFactory = LoadDatabasePlugin(efCoreConfiguration.CustomProviderOptions, appPaths);
            }
            else
            {
                var providers = GetSupportedDbProviders();
                if (!providers.TryGetValue(efCoreConfiguration.DatabaseType?.ToUpperInvariant() ?? string.Empty, out providerFactory!))
                {
                    throw new InvalidOperationException($"Jellyfin cannot find the database provider of type '{efCoreConfiguration.DatabaseType}'. Supported types are {string.Join(", ", providers.Keys)}");
                }
            }

            return providerFactory!(sp);
        });

        serviceCollection.AddSingleton<IEntityFrameworkCoreLockingBehavior>(sp =>
        {
            var efCoreConfiguration = sp.GetRequiredService<DatabaseConfigurationOptions>();
            return efCoreConfiguration.LockingBehavior switch
            {
                DatabaseLockingBehaviorTypes.Pessimistic => (IEntityFrameworkCoreLockingBehavior)ActivatorUtilities.CreateInstance<PessimisticLockBehavior>(sp),
                DatabaseLockingBehaviorTypes.Optimistic => ActivatorUtilities.CreateInstance<OptimisticLockBehavior>(sp),
                _ => ActivatorUtilities.CreateInstance<NoLockBehavior>(sp),
            };
        });

        serviceCollection.AddPooledDbContextFactory<JellyfinDbContext>((serviceProvider, opt) =>
        {
            var provider = serviceProvider.GetRequiredService<IJellyfinDatabaseProvider>();
            var efCoreConfiguration = serviceProvider.GetRequiredService<DatabaseConfigurationOptions>();
            provider.Initialise(opt, efCoreConfiguration);
            var lockingBehavior = serviceProvider.GetRequiredService<IEntityFrameworkCoreLockingBehavior>();
            lockingBehavior.Initialise(opt);
        });

        return serviceCollection;
    }
}
