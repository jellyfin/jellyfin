using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Providers.Sqlite;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JellyfinDbProviderFactory = System.Func<System.IServiceProvider, Jellyfin.Database.Implementations.IJellyfinDatabaseProvider>;

namespace Jellyfin.Server.Implementations.Extensions;

/// <summary>
/// Extensions for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    private const string ConnectionStringOverrideKey = "database:ConnectionString";

    private static IEnumerable<Type> DatabaseProviderTypes()
    {
        yield return typeof(SqliteDatabaseProvider);
        yield return typeof(PostgresDatabaseProvider);
        yield return typeof(MySqlDatabaseProvider);
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
    /// Refuses a locking behaviour that the database provider does not support. It is called when the provider is created
    /// and not when it is registered, so that the check runs where the server starts and reads the capability of the
    /// provider that was chosen.
    /// </summary>
    /// <param name="configuration">The database configuration.</param>
    /// <param name="provider">The provider that was created for it.</param>
    /// <exception cref="InvalidOperationException">The configuration asks for Pessimistic locking and the provider does not support it.</exception>
    internal static void EnsureLockingBehaviorIsSupported(DatabaseConfigurationOptions configuration, IJellyfinDatabaseProvider provider)
    {
        if (configuration.LockingBehavior == DatabaseLockingBehaviorTypes.Pessimistic && !provider.SupportsPessimisticLocking)
        {
            throw new InvalidOperationException(
                $"{nameof(DatabaseConfigurationOptions.LockingBehavior)} in database.xml is {nameof(DatabaseLockingBehaviorTypes.Pessimistic)}, "
                + $"which the database provider '{configuration.DatabaseType}' does not support. "
                + $"Set {nameof(DatabaseConfigurationOptions.LockingBehavior)} to {nameof(DatabaseLockingBehaviorTypes.NoLock)}, the default, or {nameof(DatabaseLockingBehaviorTypes.Optimistic)}.");
        }
    }

    internal static DatabaseConfigurationOptions WithConnectionStringOverride(DatabaseConfigurationOptions source, string connectionString)
    {
        var customProviderOptions = source.CustomProviderOptions;
        return new DatabaseConfigurationOptions
        {
            DatabaseType = source.DatabaseType,
            LockingBehavior = source.LockingBehavior,
            CustomProviderOptions = new CustomDatabaseOptions
            {
                PluginName = customProviderOptions?.PluginName ?? string.Empty,
                PluginAssembly = customProviderOptions?.PluginAssembly ?? string.Empty,
                ConnectionString = connectionString,
                Options = new Collection<CustomDatabaseOption>(
                    customProviderOptions?.Options?.Select(option => new CustomDatabaseOption { Key = option.Key, Value = option.Value }).ToList() ?? [])
            }
        };
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
    /// <param name="configurationManager">The server configuration manager.</param>
    /// <param name="configuration">The startup Configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddJellyfinDbContext(
        this IServiceCollection serviceCollection,
        IServerConfigurationManager configurationManager,
        IConfiguration configuration)
    {
        var efCoreConfiguration = configurationManager.GetConfiguration<DatabaseConfigurationOptions>("database");
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
                    LockingBehavior = DatabaseLockingBehaviorTypes.NoLock
                };
                configurationManager.SaveConfiguration("database", efCoreConfiguration);
            }
        }

        if (efCoreConfiguration.DatabaseType.Equals("PLUGIN_PROVIDER", StringComparison.OrdinalIgnoreCase))
        {
            if (efCoreConfiguration.CustomProviderOptions is null)
            {
                throw new InvalidOperationException("The custom database provider must declare the custom provider options to work");
            }

            providerFactory = LoadDatabasePlugin(efCoreConfiguration.CustomProviderOptions, configurationManager.ApplicationPaths);
        }
        else
        {
            var providers = GetSupportedDbProviders();
            if (!providers.TryGetValue(efCoreConfiguration.DatabaseType.ToUpperInvariant(), out providerFactory!))
            {
                throw new InvalidOperationException($"Jellyfin cannot find the database provider of type '{efCoreConfiguration.DatabaseType}'. Supported types are {string.Join(", ", providers.Keys)}");
            }
        }

        // The override is given to the provider as a copy. The cached and stored configuration must never hold it.
        var providerConfiguration = efCoreConfiguration;
        var connectionStringOverride = configuration.GetValue<string>(ConnectionStringOverrideKey);
        var hasConnectionStringOverride = !string.IsNullOrWhiteSpace(connectionStringOverride);
        if (hasConnectionStringOverride)
        {
            providerConfiguration = WithConnectionStringOverride(efCoreConfiguration, connectionStringOverride!);
        }

        serviceCollection.AddSingleton<IJellyfinDatabaseProvider>(services =>
        {
            if (hasConnectionStringOverride)
            {
                services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(ServiceCollectionExtensions).FullName!)
                    .LogInformation("The database connection string is taken from the configuration key {ConfigurationKey} instead of database.xml", ConnectionStringOverrideKey);
            }

            var provider = providerFactory!(services);
            EnsureLockingBehaviorIsSupported(efCoreConfiguration, provider);
            return provider;
        });

        switch (efCoreConfiguration.LockingBehavior)
        {
            case DatabaseLockingBehaviorTypes.NoLock:
                serviceCollection.AddSingleton<IEntityFrameworkCoreLockingBehavior, NoLockBehavior>();
                break;
            case DatabaseLockingBehaviorTypes.Pessimistic:
                serviceCollection.AddSingleton<IEntityFrameworkCoreLockingBehavior, PessimisticLockBehavior>();
                break;
            case DatabaseLockingBehaviorTypes.Optimistic:
                serviceCollection.AddSingleton<IEntityFrameworkCoreLockingBehavior, OptimisticLockBehavior>();
                break;
        }

        serviceCollection.AddPooledDbContextFactory<JellyfinDbContext>((serviceProvider, opt) =>
        {
            var provider = serviceProvider.GetRequiredService<IJellyfinDatabaseProvider>();
            provider.Initialise(opt, providerConfiguration);
            var lockingBehavior = serviceProvider.GetRequiredService<IEntityFrameworkCoreLockingBehavior>();
            lockingBehavior.Initialise(opt);
        });

        return serviceCollection;
    }
}
