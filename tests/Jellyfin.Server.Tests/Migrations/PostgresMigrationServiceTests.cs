using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.DatabaseConfiguration;
using Jellyfin.Server.Implementations.Extensions;
using Jellyfin.Server.Migrations;
using Jellyfin.Server.Migrations.Stages;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Covers selecting the PostgreSQL provider and how the migration service sets up an empty PostgreSQL database.
/// </summary>
public sealed class PostgresMigrationServiceTests : IDisposable
{
    private const string PostgresKey = "Jellyfin-PostgreSQL";

    private readonly string _root;
    private readonly ServerApplicationPaths _paths;
    private readonly List<ServiceProvider> _serviceProviders = [];
    private TestDatabase? _database;

    public PostgresMigrationServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "jellyfin-postgres-migration-service-tests", Guid.NewGuid().ToString("N"));
        _paths = new ServerApplicationPaths(
            Path.Combine(_root, "data"),
            Path.Combine(_root, "log"),
            Path.Combine(_root, "config"),
            Path.Combine(_root, "cache"),
            Path.Combine(_root, "web"));
        Directory.CreateDirectory(_paths.DataPath);
        Directory.CreateDirectory(_paths.LogDirectoryPath);
        Directory.CreateDirectory(_paths.ConfigurationDirectoryPath);
        Directory.CreateDirectory(_paths.CachePath);
    }

    [Fact]
    public void AddJellyfinDbContext_PostgresDatabaseType_RegistersThePostgresProvider()
    {
        var services = BuildServices(new DatabaseConfigurationOptions
        {
            DatabaseType = PostgresKey,
            CustomProviderOptions = new CustomDatabaseOptions
            {
                PluginName = string.Empty,
                PluginAssembly = string.Empty,
                ConnectionString = "Host=localhost;Database=jellyfin"
            }
        });

        Assert.IsType<PostgresDatabaseProvider>(services.GetRequiredService<IJellyfinDatabaseProvider>());
    }

    [Fact]
    public void AddJellyfinDbContext_UnknownDatabaseType_NamesTheSupportedProviders()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => BuildServices(new DatabaseConfigurationOptions { DatabaseType = "Jellyfin-Unknown" }));

        Assert.Contains("Jellyfin-SQLite", exception.Message, StringComparison.Ordinal);
        Assert.Contains(PostgresKey, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Baseline_SortsBeforeEveryCodeRoutine()
    {
        var routines = CodeMigrations().Select(e => e.BuildCodeMigrationId()).ToList();

        Assert.NotEmpty(routines);
        Assert.All(routines, id => Assert.True(string.CompareOrdinal(PostgresMigrationBaseline.MigrationId, id) < 0, id));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task CheckFirstTimeRunOrMigration_EmptyDatabase_RecordsTheRoutinesThatSkipASetup()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService();

        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());

        var expected = CodeMigrations()
            .Where(e => !e.Metadata.RunMigrationOnSetup)
            .Select(e => e.BuildCodeMigrationId())
            .Order(StringComparer.Ordinal);
        Assert.Equal(expected, await GetAppliedMigrationIdsAsync());
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task MigrateStepAsync_CoreStageOnSeededDatabase_CreatesTheSchemaAndTheAggregate()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService();
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());

        var applied = await service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]);

        Assert.True(applied);
        Assert.Contains(PostgresMigrationBaseline.MigrationId, await GetAppliedMigrationIdsAsync());
        await using var context = await CreateDbContextAsync();
        Assert.Equal(0, await context.Users.CountAsync(TestContext.Current.CancellationToken));
        Assert.True(await context.Database.SqlQuery<bool>($"SELECT to_regprocedure('min(uuid)') IS NOT NULL AS \"Value\"").SingleAsync(TestContext.Current.CancellationToken));
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task MigrateStepAsync_CoreStageOnSeededDatabase_LeavesNothingForASecondRun()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService();
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]);
        var applied = await GetAppliedMigrationIdsAsync();

        var again = await service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]);

        Assert.False(again);
        Assert.Equal(applied, await GetAppliedMigrationIdsAsync());
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task CheckFirstTimeRunOrMigration_RestartOfASetUpServer_ReusesTheDatabase()
    {
        WriteServerConfiguration(wizardCompleted: false);
        var first = CreateService();
        await first.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        await first.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]);
        var applied = await GetAppliedMigrationIdsAsync();
        WriteServerConfiguration(wizardCompleted: true);

        var restarted = CreateService();
        await restarted.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        var changed = await restarted.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]);

        Assert.False(changed);
        Assert.Equal(applied, await GetAppliedMigrationIdsAsync());
    }

    [ProviderFact(TestDbProvider.Postgres)]
    public async Task CheckFirstTimeRunOrMigration_SetUpServerOnEmptyDatabase_Throws()
    {
        WriteServerConfiguration(wizardCompleted: true);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.Contains("the migration history of the database is empty", exception.Message, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        foreach (var serviceProvider in _serviceProviders)
        {
            serviceProvider.Dispose();
        }

        _database?.Dispose();
        try
        {
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // Best effort, a locked file must not fail the test.
        }
    }

    private static IEnumerable<CodeMigration> CodeMigrations()
    {
        return typeof(JellyfinMigrationService).Assembly.GetTypes()
            .Select(type => (Type: type, Metadata: type.GetCustomAttribute<JellyfinMigrationAttribute>()))
            .Where(e => e.Metadata is not null)
            .Select(e => new CodeMigration(e.Type, e.Metadata!, null));
    }

    private void WriteServerConfiguration(bool wizardCompleted)
    {
        new MyXmlSerializer().SerializeToFile(new ServerConfiguration { IsStartupWizardCompleted = wizardCompleted }, _paths.SystemConfigurationFilePath);
    }

    private ServiceProvider BuildServices(DatabaseConfigurationOptions databaseConfiguration)
    {
        var configurationManager = new ServerConfigurationManager(_paths, NullLoggerFactory.Instance, new MyXmlSerializer());
        configurationManager.AddParts([new DatabaseConfigurationFactory()]);
        configurationManager.SaveConfiguration("database", databaseConfiguration);
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddJellyfinDbContext(configurationManager, new ConfigurationBuilder().Build())
            .AddSingleton<IApplicationPaths>(_paths)
            .RegisterStartupLogger()
            .BuildServiceProvider();
        _serviceProviders.Add(serviceProvider);
        return serviceProvider;
    }

    private JellyfinMigrationService CreateService()
    {
        _database ??= PostgresTestBackend.Instance.CreateEmpty();
        var serviceProvider = BuildServices(new DatabaseConfigurationOptions
        {
            DatabaseType = PostgresKey,
            CustomProviderOptions = new CustomDatabaseOptions
            {
                PluginName = string.Empty,
                PluginAssembly = string.Empty,
                ConnectionString = RelationalOptionsExtension.Extract(_database.Options).ConnectionString!
            }
        });

        var factory = serviceProvider.GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
        serviceProvider.GetRequiredService<IJellyfinDatabaseProvider>().DbContextFactory = factory;
        return ActivatorUtilities.CreateInstance<JellyfinMigrationService>(serviceProvider);
    }

    private async Task<JellyfinDbContext> CreateDbContextAsync()
    {
        var factory = _serviceProviders[^1].GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
        return await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
    }

    private async Task<string[]> GetAppliedMigrationIdsAsync()
    {
        await using var context = await CreateDbContextAsync();
        var applied = await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        return applied.Order(StringComparer.Ordinal).ToArray();
    }
}
