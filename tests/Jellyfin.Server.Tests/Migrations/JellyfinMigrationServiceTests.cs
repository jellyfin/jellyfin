using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Emby.Server.Implementations;
using Emby.Server.Implementations.Configuration;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.DatabaseConfiguration;
using Jellyfin.Server.Implementations.Extensions;
using Jellyfin.Server.Migrations;
using Jellyfin.Server.Migrations.Stages;
using Jellyfin.Server.ServerSetupApp;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Covers how the migration service treats the database of a server that has been set up before.
/// </summary>
public sealed class JellyfinMigrationServiceTests : IDisposable
{
    private readonly string _root;
    private readonly ServerApplicationPaths _paths;
    private readonly List<ServiceProvider> _serviceProviders = [];

    public JellyfinMigrationServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "jellyfin-migration-service-tests", Guid.NewGuid().ToString("N"));
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

    private string DatabasePath => Path.Combine(_paths.DataPath, "jellyfin.db");

    [Fact]
    public async Task CheckFirstTimeRunOrMigration_SetUpServerWithoutDatabase_ThrowsWithoutCreatingIt()
    {
        WriteServerConfiguration(wizardCompleted: true);
        var service = CreateService();
        var systemConfiguration = await File.ReadAllBytesAsync(_paths.SystemConfigurationFilePath, TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.Contains("the database does not exist", exception.Message, StringComparison.Ordinal);
        Assert.Contains(_paths.SystemConfigurationFilePath, exception.Message, StringComparison.Ordinal);
        Assert.Empty(Directory.GetFiles(_paths.DataPath, "jellyfin.db*"));
        Assert.Equal(systemConfiguration, await File.ReadAllBytesAsync(_paths.SystemConfigurationFilePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CheckFirstTimeRunOrMigration_SetUpServerWithEmptyDatabaseFile_Throws()
    {
        WriteServerConfiguration(wizardCompleted: true);
        await File.WriteAllBytesAsync(DatabasePath, [], TestContext.Current.CancellationToken);
        var service = CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.Contains("the database has no migration history", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckFirstTimeRunOrMigration_SetUpServerWithEmptyHistory_Throws()
    {
        WriteServerConfiguration(wizardCompleted: true);
        var service = CreateService();
        await using (var context = await CreateDbContextAsync())
        {
            await context.GetService<IHistoryRepository>().CreateIfNotExistsAsync(TestContext.Current.CancellationToken);
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions()));

        Assert.Contains("the migration history of the database is empty", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CheckFirstTimeRunOrMigration_SetUpServerWithHistory_Passes()
    {
        WriteServerConfiguration(wizardCompleted: false);
        await CreateService().CheckFirstTimeRunOrMigration(_paths, new StartupOptions());
        var applied = await GetAppliedMigrationIdsAsync();
        WriteServerConfiguration(wizardCompleted: true);

        await CreateService().CheckFirstTimeRunOrMigration(_paths, new StartupOptions());

        Assert.NotEmpty(applied);
        Assert.Equal(applied, await GetAppliedMigrationIdsAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MigrateStepAsync_MaxActiveVideoStreams_PreservesExistingUserAndIsIdempotent(bool ancestorIndexAlreadyUpgraded)
    {
        const string previousMigration = "20261002210711_MakeOwnerIdIndexesPartial";
        const string ancestorIndexMigration = "20261024092111_CoverAncestorIdsParentIndex";
        const string newMigration = "20261117101651_AddMaxActiveVideoStreams";
        // Seed completed code migrations without applying the pending database migration.
        WriteServerConfiguration(wizardCompleted: false);
        var service = CreateService();
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());

        await using (var context = await CreateDbContextAsync())
        {
            await context.GetService<IMigrator>().MigrateAsync(
                ancestorIndexAlreadyUpgraded ? ancestorIndexMigration : previousMigration,
                TestContext.Current.CancellationToken);
            await context.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "Users" (
                    "Id", "Username", "NormalizedUsername", "AuthenticationProviderId", "PasswordResetProviderId",
                    "AudioLanguagePreference", "SubtitleLanguagePreference", "CastReceiverId", "Password",
                    "LastActivityDate", "LastLoginDate", "LoginAttemptsBeforeLockout",
                    "MaxActiveSessions", "InvalidLoginAttemptCount", "MaxParentalRatingScore", "MaxParentalRatingSubScore",
                    "RemoteClientBitrateLimit", "InternalId", "RowVersion",
                    "DisplayCollectionsView", "DisplayMissingEpisodes", "EnableAutoLogin", "EnableLocalPassword",
                    "EnableNextEpisodeAutoPlay", "EnableUserPreferenceAccess", "HidePlayedInLatest", "MustUpdatePassword",
                    "PlayDefaultAudioTrack", "RememberAudioSelections", "RememberSubtitleSelections", "SubtitleMode", "SyncPlayAccess")
                VALUES (
                    '11111111-1111-1111-1111-111111111111', 'MigrationSentinel', 'SENTINEL-NORMALIZED', 'MigrationTest', 'MigrationTest',
                    'ita', 'eng', 'sentinel-receiver', 'sentinel-password',
                    '2026-10-01 12:00:00', '2026-09-30 11:00:00', 5,
                    3, 2, 15, 3,
                    1000000, 7, 11,
                    1, 0, 0, 1,
                    1, 1, 1, 0,
                    1, 1, 1, 1, 1);
                UPDATE "__EFMigrationsHistory"
                SET "ProductVersion" = '10.0.11'
                WHERE "MigrationId" = '20261002210711_MakeOwnerIdIndexesPartial';
                """,
                TestContext.Current.CancellationToken);
        }

        var userBefore = await ReadUserAsync();
        var historyBefore = await GetAppliedMigrationHistoryAsync();
        Assert.Equal(32, userBefore.Count);
        Assert.DoesNotContain("MaxActiveVideoStreams", userBefore.Keys);
        Assert.DoesNotContain(historyBefore, row => row.MigrationId == newMigration);
        WriteServerConfiguration(wizardCompleted: true);
        await service.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());

        Assert.True(await service.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));

        var userAfter = await ReadUserAsync();
        var historyAfter = await GetAppliedMigrationHistoryAsync();
        Assert.Equal(userBefore.Count + 1, userAfter.Count);
        foreach (var column in userBefore)
        {
            Assert.Equal(column.Value, userAfter[column.Key]);
        }

        Assert.Equal(0L, Assert.IsType<long>(userAfter["MaxActiveVideoStreams"]));
        Assert.Equal(historyBefore, historyAfter.Take(historyBefore.Length));
        Assert.Equal(
            ancestorIndexAlreadyUpgraded ? [newMigration] : new[] { ancestorIndexMigration, newMigration },
            historyAfter.Skip(historyBefore.Length).Select(row => row.MigrationId));

        await using (var context = await CreateDbContextAsync())
        {
            var indexColumns = await context.Database.SqlQueryRaw<string>(
                "SELECT name AS Value FROM pragma_index_info('IX_AncestorIds_ParentItemId_ItemId') ORDER BY seqno")
                .ToArrayAsync(TestContext.Current.CancellationToken);
            Assert.Equal(["ParentItemId", "ItemId"], indexColumns);
            Assert.False(context.Database.HasPendingModelChanges());
            await context.Database.ExecuteSqlRawAsync("UPDATE \"Users\" SET \"MaxActiveVideoStreams\" = 2;", TestContext.Current.CancellationToken);
        }

        var restartedService = CreateService();
        await restartedService.CheckFirstTimeRunOrMigration(_paths, new StartupOptions());

        Assert.False(await restartedService.MigrateStepAsync(JellyfinMigrationStageTypes.CoreInitialisation, _serviceProviders[^1]));
        Assert.Equal(historyAfter, await GetAppliedMigrationHistoryAsync());
        Assert.Equal(2L, Assert.IsType<long>((await ReadUserAsync())["MaxActiveVideoStreams"]));
    }

    [Fact]
    public async Task CheckFirstTimeRunOrMigration_SeedSystemOnSetUpServerWithoutDatabase_SeedsCodeMigrations()
    {
        WriteServerConfiguration(wizardCompleted: true);

        await CreateService().CheckFirstTimeRunOrMigration(_paths, new StartupOptions { StartupMode = Configuration.StartupMode.SeedSystem });

        Assert.NotEmpty(await GetAppliedMigrationIdsAsync());
    }

    [Fact]
    public async Task CheckFirstTimeRunOrMigration_MigrateSystemOnSetUpServerWithoutDatabase_Passes()
    {
        WriteServerConfiguration(wizardCompleted: true);

        await CreateService().CheckFirstTimeRunOrMigration(_paths, new StartupOptions { StartupMode = Configuration.StartupMode.MigrateSystem });
    }

    [Fact]
    public async Task CheckFirstTimeRunOrMigration_RestoreArchiveOnSetUpServerWithoutDatabase_Passes()
    {
        WriteServerConfiguration(wizardCompleted: true);

        await CreateService().CheckFirstTimeRunOrMigration(_paths, new StartupOptions { RestoreArchive = Path.Combine(_root, "backup.zip") });
    }

    public void Dispose()
    {
        foreach (var serviceProvider in _serviceProviders)
        {
            serviceProvider.Dispose();
        }

        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // Best effort, a locked file must not fail the test.
        }
    }

    private void WriteServerConfiguration(bool wizardCompleted)
    {
        new MyXmlSerializer().SerializeToFile(new ServerConfiguration { IsStartupWizardCompleted = wizardCompleted }, _paths.SystemConfigurationFilePath);
    }

    private JellyfinMigrationService CreateService()
    {
        var configurationManager = new ServerConfigurationManager(_paths, NullLoggerFactory.Instance, new MyXmlSerializer());
        configurationManager.AddParts([new DatabaseConfigurationFactory()]);
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddJellyfinDbContext(configurationManager, new ConfigurationBuilder().Build())
            .AddSingleton<IApplicationPaths>(_paths)
            .RegisterStartupLogger()
            .BuildServiceProvider();
        _serviceProviders.Add(serviceProvider);

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

    private async Task<(string MigrationId, string ProductVersion)[]> GetAppliedMigrationHistoryAsync()
    {
        await using var context = await CreateDbContextAsync();
        var applied = await context.GetService<IHistoryRepository>().GetAppliedMigrationsAsync(TestContext.Current.CancellationToken);
        return applied.Select(row => (row.MigrationId, row.ProductVersion)).OrderBy(row => row.MigrationId, StringComparer.Ordinal).ToArray();
    }

    private async Task<Dictionary<string, object>> ReadUserAsync()
    {
        await using var context = await CreateDbContextAsync();
        await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT * FROM \"Users\";";
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        Assert.True(await reader.ReadAsync(TestContext.Current.CancellationToken));
        var values = new Dictionary<string, object>(StringComparer.Ordinal);
        for (var index = 0; index < reader.FieldCount; index++)
        {
            values.Add(reader.GetName(index), reader.GetValue(index));
        }

        Assert.False(await reader.ReadAsync(TestContext.Current.CancellationToken));
        return values;
    }
}
