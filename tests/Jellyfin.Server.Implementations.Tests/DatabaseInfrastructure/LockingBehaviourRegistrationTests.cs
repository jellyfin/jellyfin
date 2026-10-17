using System;
using System.IO;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.DatabaseConfiguration;
using Jellyfin.Server.Implementations.Extensions;
using Jellyfin.Server.Implementations.Tests.Extensions;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// Holds what a database provider says of the locking behaviours it supports, and what the server does with it when it
/// starts. Nothing here connects: the providers are created and, where a test needs the server's own registration, only
/// registered and resolved.
/// </summary>
/// <remarks>
/// The server starts by resolving the database context factory and then the provider, as <c>PrepareDatabaseProvider</c>
/// does, so that is what the tests do. An engine whose provider the server does not register yet cannot be started that
/// way, so its provider is created and checked directly. The trait of the engine selects between the two, and the tests
/// need no change when the engine is registered.
/// </remarks>
public sealed class LockingBehaviourRegistrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "jellyfin-locking-registration-tests", Guid.NewGuid().ToString("N"));

    public static TheoryData<TestDbProvider> Engines()
    {
        var engines = new TheoryData<TestDbProvider>();
        foreach (var provider in Enum.GetValues<TestDbProvider>())
        {
            engines.Add(provider);
        }

        return engines;
    }

    public static TheoryData<TestDbProvider> ServerEngines()
    {
        var engines = new TheoryData<TestDbProvider>();
        foreach (var provider in Enum.GetValues<TestDbProvider>())
        {
            if (!EngineTraits.For(provider).IsEmbedded)
            {
                engines.Add(provider);
            }
        }

        return engines;
    }

    public static TheoryData<TestDbProvider> RegisteredServerEngines()
    {
        var engines = new TheoryData<TestDbProvider>();
        foreach (var provider in Enum.GetValues<TestDbProvider>())
        {
            var traits = EngineTraits.For(provider);
            if (!traits.IsEmbedded && traits.IsRegistered)
            {
                engines.Add(provider);
            }
        }

        return engines;
    }

    public static TheoryData<TestDbProvider, DatabaseLockingBehaviorTypes> EnginesWithTheBehavioursEveryEngineSupports()
    {
        var rows = new TheoryData<TestDbProvider, DatabaseLockingBehaviorTypes>();
        foreach (var provider in Enum.GetValues<TestDbProvider>())
        {
            rows.Add(provider, DatabaseLockingBehaviorTypes.NoLock);
            rows.Add(provider, DatabaseLockingBehaviorTypes.Optimistic);
        }

        return rows;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }
    }

    [Theory]
    [MemberData(nameof(Engines))]
    public void SupportsPessimisticLocking_IsTrueOnlyForSqlite(TestDbProvider engine)
    {
        var traits = EngineTraits.For(engine);

        var provider = CreateProvider(traits);

        // The embedded engine is the one with a single writer, which the process-wide lock of the behaviour stands in for.
        Assert.Equal(traits.IsEmbedded, provider.SupportsPessimisticLocking);
    }

    [Theory]
    [MemberData(nameof(ServerEngines))]
    public void AddJellyfinDbContext_PessimisticOnAServerEngine_RefusesToStartAndNamesTheSetting(TestDbProvider engine)
    {
        var traits = EngineTraits.For(engine);

        var refusal = Record.Exception(() => Start(traits, DatabaseLockingBehaviorTypes.Pessimistic));

        var exception = Assert.IsType<InvalidOperationException>(refusal);
        Assert.Contains(nameof(DatabaseConfigurationOptions.LockingBehavior), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DatabaseLockingBehaviorTypes.Pessimistic), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DatabaseLockingBehaviorTypes.NoLock), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DatabaseLockingBehaviorTypes.Optimistic), exception.Message, StringComparison.Ordinal);
        Assert.Contains($"'{traits.ProviderKey}'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddJellyfinDbContext_PessimisticOnSqlite_Starts()
    {
        var traits = EngineTraits.For(TestDbProvider.Sqlite);

        var exception = Record.Exception(() => Start(traits, DatabaseLockingBehaviorTypes.Pessimistic));

        Assert.Null(exception);
    }

    [Theory]
    [MemberData(nameof(EnginesWithTheBehavioursEveryEngineSupports))]
    public void AddJellyfinDbContext_NoLockOrOptimistic_StartsOnEveryEngine(TestDbProvider engine, DatabaseLockingBehaviorTypes lockingBehaviour)
    {
        var traits = EngineTraits.For(engine);

        var exception = Record.Exception(() => Start(traits, lockingBehaviour));

        Assert.Null(exception);
    }

    // The refusal is made when the provider is created, which is when the server resolves it, and not when the database
    // context is registered: the registration neither creates the provider nor reads its capability.
    [Theory]
    [MemberData(nameof(RegisteredServerEngines))]
    public void AddJellyfinDbContext_PessimisticOnAServerEngine_RegistrationAloneDoesNotThrow(TestDbProvider engine)
    {
        var traits = EngineTraits.For(engine);

        var exception = Record.Exception(() => Register(traits, DatabaseLockingBehaviorTypes.Pessimistic));

        Assert.Null(exception);
    }

    // A provider from a plugin does not say anything of locking, so it keeps what it has had: the behaviour is allowed.
    [Fact]
    public void EnsureLockingBehaviorIsSupported_PluginProviderThatKeepsTheDefault_StartsUnderPessimistic()
    {
        IJellyfinDatabaseProvider provider = new RecordingDatabaseProvider([]);
        var configuration = new DatabaseConfigurationOptions
        {
            DatabaseType = "PLUGIN_PROVIDER",
            LockingBehavior = DatabaseLockingBehaviorTypes.Pessimistic
        };

        var exception = Record.Exception(() => ServiceCollectionExtensions.EnsureLockingBehaviorIsSupported(configuration, provider));

        Assert.Null(exception);
        Assert.True(provider.SupportsPessimisticLocking);
    }

    private static IJellyfinDatabaseProvider CreateProvider(EngineTraits traits)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(Mock.Of<IApplicationPaths>())
            .BuildServiceProvider();
        return (IJellyfinDatabaseProvider)ActivatorUtilities.CreateInstance(services, traits.ProviderType);
    }

    private static DatabaseConfigurationOptions CreateConfiguration(EngineTraits traits, DatabaseLockingBehaviorTypes lockingBehaviour)
        => new()
        {
            DatabaseType = traits.ProviderKey,
            LockingBehavior = lockingBehaviour,
            CustomProviderOptions = new CustomDatabaseOptions
            {
                PluginName = string.Empty,
                PluginAssembly = string.Empty,
                ConnectionString = "Server=localhost;Database=jellyfin_locking",

                // The providers of MySQL and MariaDB take the version of the server from here, as they would otherwise
                // connect to find it. The other providers do not read it.
                Options = [new CustomDatabaseOption { Key = "server-version", Value = traits.ServerMarker == "MariaDB" ? "11.4.7-mariadb" : "8.4-mysql" }]
            }
        };

    // Starts the server as far as the choice of the provider goes: the factory first, then the provider, as the startup does.
    private void Start(EngineTraits traits, DatabaseLockingBehaviorTypes lockingBehaviour)
    {
        if (!traits.IsRegistered)
        {
            ServiceCollectionExtensions.EnsureLockingBehaviorIsSupported(CreateConfiguration(traits, lockingBehaviour), CreateProvider(traits));
            return;
        }

        using var services = Register(traits, lockingBehaviour).BuildServiceProvider();
        _ = services.GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
        _ = services.GetRequiredService<IJellyfinDatabaseProvider>();
    }

    private ServiceCollection Register(EngineTraits traits, DatabaseConfigurationOptions configuration)
    {
        var root = Path.Combine(_root, Guid.NewGuid().ToString("N"));
        var paths = new ServerApplicationPaths(
            Path.Combine(root, "data"),
            Path.Combine(root, "log"),
            Path.Combine(root, "config"),
            Path.Combine(root, "cache"),
            Path.Combine(root, "web"));
        Directory.CreateDirectory(paths.ConfigurationDirectoryPath);
        var configurationManager = new ServerConfigurationManager(paths, NullLoggerFactory.Instance, new MyXmlSerializer());
        configurationManager.AddParts([new DatabaseConfigurationFactory()]);
        configurationManager.SaveConfiguration("database", configuration);

        var services = new ServiceCollection();
        services
            .AddLogging()
            .AddSingleton<IApplicationPaths>(paths)
            .AddJellyfinDbContext(configurationManager, new ConfigurationBuilder().Build());
        return services;
    }

    private ServiceCollection Register(EngineTraits traits, DatabaseLockingBehaviorTypes lockingBehaviour)
        => Register(traits, CreateConfiguration(traits, lockingBehaviour));
}
