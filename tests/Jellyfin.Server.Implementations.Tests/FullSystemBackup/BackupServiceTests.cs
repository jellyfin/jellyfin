using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.CustomNetflix;
using Jellyfin.Server.Implementations.FullSystemBackup;
using MediaBrowser.Controller;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.FullSystemBackup;

public sealed class BackupServiceTests
{
    [Fact]
    public async Task RestoreBackupAsync_InvalidDatabaseJson_DoesNotMutateFiles()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var archivePath = Path.Combine(testDirectory, "invalid.zip");
            CreateArchive(archivePath, "[", includeConfig: true);
            var paths = CreateApplicationPaths(testDirectory);
            var configurationFile = Path.Combine(paths.Object.ConfigurationDirectoryPath, "system.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(configurationFile)!);
            await File.WriteAllTextAsync(configurationFile, "original", TestContext.Current.CancellationToken);
            var databaseProvider = new Mock<IJellyfinDatabaseProvider>();
            var service = CreateService(paths, databaseProvider, new Mock<IDbContextFactory<JellyfinDbContext>>());

            await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => service.RestoreBackupAsync(archivePath));

            Assert.Equal("original", await File.ReadAllTextAsync(configurationFile, TestContext.Current.CancellationToken));
            databaseProvider.Verify(
                provider => provider.MigrationBackupFast(It.IsAny<CancellationToken>()),
                Times.Never);
        }
        finally
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_DatabaseFailure_RestoresRollbackBackup()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var archivePath = Path.Combine(testDirectory, "valid.zip");
            CreateArchive(archivePath, "[]", includeConfig: true);
            var paths = CreateApplicationPaths(testDirectory);
            var configurationFile = Path.Combine(paths.Object.ConfigurationDirectoryPath, "system.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(configurationFile)!);
            await File.WriteAllTextAsync(configurationFile, "original", TestContext.Current.CancellationToken);
            var databaseProvider = new Mock<IJellyfinDatabaseProvider>();
            databaseProvider
                .Setup(provider => provider.MigrationBackupFast(It.IsAny<CancellationToken>()))
                .ReturnsAsync("rollback");
            databaseProvider
                .Setup(provider => provider.RestoreBackupFast("rollback", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            databaseProvider
                .Setup(provider => provider.DeleteBackup("rollback"))
                .Returns(Task.CompletedTask);
            var dbContextFactory = new Mock<IDbContextFactory<JellyfinDbContext>>();
            dbContextFactory
                .Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("restore failed"));
            var service = CreateService(paths, databaseProvider, dbContextFactory);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RestoreBackupAsync(archivePath));

            databaseProvider.Verify(
                provider => provider.RestoreBackupFast("rollback", It.IsAny<CancellationToken>()),
                Times.Once);
            databaseProvider.Verify(provider => provider.DeleteBackup("rollback"), Times.Once);
            Assert.Equal("original", await File.ReadAllTextAsync(configurationFile, TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_MissingDatabaseTable_DoesNotMutateFiles()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var archivePath = Path.Combine(testDirectory, "missing-table.zip");
            CreateArchive(archivePath, "[]", includeConfig: true, omitFirstTable: true);
            var paths = CreateApplicationPaths(testDirectory);
            var configurationFile = Path.Combine(paths.Object.ConfigurationDirectoryPath, "system.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(configurationFile)!);
            await File.WriteAllTextAsync(configurationFile, "original", TestContext.Current.CancellationToken);
            var databaseProvider = new Mock<IJellyfinDatabaseProvider>();
            var service = CreateService(paths, databaseProvider, new Mock<IDbContextFactory<JellyfinDbContext>>());

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RestoreBackupAsync(archivePath));

            Assert.Equal("original", await File.ReadAllTextAsync(configurationFile, TestContext.Current.CancellationToken));
            databaseProvider.Verify(
                provider => provider.MigrationBackupFast(It.IsAny<CancellationToken>()),
                Times.Never);
        }
        finally
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_InvalidConfigurationXml_DoesNotMutateFiles()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var archivePath = Path.Combine(testDirectory, "invalid-xml.zip");
            CreateArchive(archivePath, "[]", includeConfig: true, configXml: "<ServerConfiguration>");
            var paths = CreateApplicationPaths(testDirectory);
            var configurationFile = Path.Combine(paths.Object.ConfigurationDirectoryPath, "system.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(configurationFile)!);
            await File.WriteAllTextAsync(configurationFile, "original", TestContext.Current.CancellationToken);
            var databaseProvider = new Mock<IJellyfinDatabaseProvider>();
            var service = CreateService(paths, databaseProvider, new Mock<IDbContextFactory<JellyfinDbContext>>());

            await Assert.ThrowsAsync<XmlException>(() => service.RestoreBackupAsync(archivePath));

            Assert.Equal("original", await File.ReadAllTextAsync(configurationFile, TestContext.Current.CancellationToken));
            databaseProvider.Verify(
                provider => provider.MigrationBackupFast(It.IsAny<CancellationToken>()),
                Times.Never);
        }
        finally
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_MissingCustomNetflixDump_DoesNotMutateFiles()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var archivePath = Path.Combine(testDirectory, "missing-customnetflix.zip");
            CreateArchive(archivePath, "[]", includeConfig: true, customNetflixDatabase: true);
            var paths = CreateApplicationPaths(testDirectory);
            var configurationFile = Path.Combine(paths.Object.ConfigurationDirectoryPath, "system.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(configurationFile)!);
            await File.WriteAllTextAsync(configurationFile, "original", TestContext.Current.CancellationToken);
            var databaseProvider = new Mock<IJellyfinDatabaseProvider>();
            var service = CreateService(paths, databaseProvider, new Mock<IDbContextFactory<JellyfinDbContext>>());

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RestoreBackupAsync(archivePath));

            Assert.Equal("original", await File.ReadAllTextAsync(configurationFile, TestContext.Current.CancellationToken));
            databaseProvider.Verify(
                provider => provider.MigrationBackupFast(It.IsAny<CancellationToken>()),
                Times.Never);
        }
        finally
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_LegacyManifestMissingCurrentTable_ReachesDatabaseRestore()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var archivePath = Path.Combine(testDirectory, "legacy.zip");
            CreateArchive(
                archivePath,
                "[]",
                includeConfig: true,
                omitFirstTable: true,
                legacyDatabaseTableManifest: true);
            var paths = CreateApplicationPaths(testDirectory);
            var configurationFile = Path.Combine(paths.Object.ConfigurationDirectoryPath, "system.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(configurationFile)!);
            await File.WriteAllTextAsync(configurationFile, "original", TestContext.Current.CancellationToken);
            var databaseProvider = new Mock<IJellyfinDatabaseProvider>();
            databaseProvider
                .Setup(provider => provider.MigrationBackupFast(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("database restore reached"));
            var service = CreateService(paths, databaseProvider, new Mock<IDbContextFactory<JellyfinDbContext>>());

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RestoreBackupAsync(archivePath));

            Assert.Equal("database restore reached", exception.Message);
            databaseProvider.Verify(
                provider => provider.MigrationBackupFast(It.IsAny<CancellationToken>()),
                Times.Once);
            Assert.Equal("original", await File.ReadAllTextAsync(configurationFile, TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_RollbackFailurePreservesNativeRollbackBackup()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var archivePath = Path.Combine(testDirectory, "rollback-failure.zip");
            CreateArchive(archivePath, "[]", includeConfig: true);
            var paths = CreateApplicationPaths(testDirectory);
            var databaseProvider = new Mock<IJellyfinDatabaseProvider>();
            databaseProvider
                .Setup(provider => provider.MigrationBackupFast(It.IsAny<CancellationToken>()))
                .ReturnsAsync("rollback");
            databaseProvider
                .Setup(provider => provider.RestoreBackupFast("rollback", It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("rollback failed"));
            var dbContextFactory = new Mock<IDbContextFactory<JellyfinDbContext>>();
            dbContextFactory
                .Setup(factory => factory.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("restore failed"));
            var service = CreateService(paths, databaseProvider, dbContextFactory);

            await Assert.ThrowsAsync<AggregateException>(() => service.RestoreBackupAsync(archivePath));

            databaseProvider.Verify(
                provider => provider.RestoreBackupFast("rollback", It.IsAny<CancellationToken>()),
                Times.Once);
            databaseProvider.Verify(provider => provider.DeleteBackup("rollback"), Times.Never);
        }
        finally
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_FileRollbackFailurePreservesStagingRollbackFiles()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var archivePath = Path.Combine(testDirectory, "file-rollback-failure.zip");
            CreateArchive(archivePath, "[]", includeConfig: true);
            var paths = CreateApplicationPaths(testDirectory);
            var configurationFile = Path.Combine(paths.Object.ConfigurationDirectoryPath, "system.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(configurationFile)!);
            await File.WriteAllTextAsync(configurationFile, "original", TestContext.Current.CancellationToken);
            var databaseProvider = new Mock<IJellyfinDatabaseProvider>();
            databaseProvider
                .Setup(provider => provider.MigrationBackupFast(It.IsAny<CancellationToken>()))
                .Callback(() =>
                {
                    File.Delete(configurationFile);
                    Directory.CreateDirectory(configurationFile);
                })
                .ThrowsAsync(new InvalidOperationException("restore failed"));
            var service = CreateService(
                paths,
                databaseProvider,
                new Mock<IDbContextFactory<JellyfinDbContext>>());

            await Assert.ThrowsAsync<AggregateException>(() => service.RestoreBackupAsync(archivePath));

            var preservedStaging = Assert.Single(
                Directory.GetDirectories(paths.Object.TempDirectory, "restore-*", SearchOption.TopDirectoryOnly));
            Assert.NotEmpty(
                Directory.GetFiles(
                    Path.Combine(preservedStaging, ".rollback"),
                    "*",
                    SearchOption.TopDirectoryOnly));
        }
        finally
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public async Task RestoreBackupAsync_CustomNetflixPreflightFailureDoesNotMutateFiles()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var archivePath = Path.Combine(testDirectory, "invalid-customnetflix.zip");
            CreateArchive(
                archivePath,
                "[]",
                includeConfig: true,
                customNetflixDatabase: true,
                includeCustomNetflixDump: true);
            var paths = CreateApplicationPaths(testDirectory);
            var configurationFile = Path.Combine(paths.Object.ConfigurationDirectoryPath, "system.xml");
            Directory.CreateDirectory(Path.GetDirectoryName(configurationFile)!);
            await File.WriteAllTextAsync(configurationFile, "original", TestContext.Current.CancellationToken);
            var databaseProvider = new Mock<IJellyfinDatabaseProvider>();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["CustomNetflix:PgRestorePath"] = Path.Combine(testDirectory, "missing-pg-restore")
                })
                .Build();
            var service = CreateService(
                paths,
                databaseProvider,
                new Mock<IDbContextFactory<JellyfinDbContext>>(),
                configuration);

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RestoreBackupAsync(archivePath));

            Assert.Equal("original", await File.ReadAllTextAsync(configurationFile, TestContext.Current.CancellationToken));
            databaseProvider.Verify(
                provider => provider.MigrationBackupFast(It.IsAny<CancellationToken>()),
                Times.Never);
        }
        finally
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public void PostgreSqlArguments_SkipEmptyRollbackAndUseAtomicRestore()
    {
        var backupArguments = BackupService.BuildCustomNetflixDumpArguments(
            "custom.schema",
            "backup.pgdump");
        var restoreArguments = BackupService.BuildCustomNetflixRestoreArguments(
            "custom.schema",
            "backup.pgdump");

        Assert.False(BackupService.ShouldCreateCustomNetflixDump(hasTables: false, allowNoTables: true));
        Assert.True(BackupService.ShouldCreateCustomNetflixDump(hasTables: true, allowNoTables: true));
        Assert.Throws<InvalidOperationException>(
            () => BackupService.ShouldCreateCustomNetflixDump(hasTables: false, allowNoTables: false));
        Assert.Contains("--strict-names", backupArguments);
        Assert.Contains("--table=\"custom.schema\".cnx_*", backupArguments);
        Assert.Contains("--schema=custom.schema", restoreArguments);
        Assert.Contains("--strict-names", restoreArguments);
        Assert.Contains("--single-transaction", restoreArguments);
        Assert.Contains("--exit-on-error", restoreArguments);
    }

    [Fact]
    public void ValidateCustomNetflixDumpToc_AcceptsOnlyScopedCnxObjects()
    {
        const string ScopedToc =
            """
            ;
            ;     Format: CUSTOM
            10; 1259 100 TABLE custom.schema cnx_profiles jellyfin
            11; 0 100 TABLE DATA custom.schema cnx_profiles jellyfin
            12; 2606 101 CONSTRAINT custom.schema cnx_profiles cnx_profiles_pkey jellyfin
            13; 1259 102 INDEX custom.schema idx_recent jellyfin
            14; 0 0 COMMENT custom.schema TABLE cnx_profiles jellyfin
            """;
        const string CompleteToc =
            """
            ;
            ;     Format: CUSTOM
            1; 0 0 ENCODING - UTF8
            2; 0 0 STDSTRINGS - stdstrings
            3; 0 0 SEARCHPATH - SEARCHPATH
            10; 1259 100 TABLE custom.schema cnx_profiles jellyfin
            11; 0 100 TABLE DATA custom.schema cnx_profiles jellyfin
            12; 2606 101 CONSTRAINT custom.schema cnx_profiles cnx_profiles_pkey jellyfin
            13; 1259 102 INDEX custom.schema idx_recent jellyfin
            14; 0 0 COMMENT custom.schema TABLE cnx_profiles jellyfin
            """;

        BackupService.ValidateCustomNetflixDumpToc(CompleteToc, ScopedToc, "custom.schema");
    }

    [Fact]
    public void ValidateCustomNetflixDumpToc_AcceptsSchemaContainingSpaces()
    {
        const string Toc =
            """
            ;     Format: CUSTOM
            10; 1259 100 TABLE custom schema cnx_profiles jellyfin
            11; 0 100 TABLE DATA custom schema cnx_profiles jellyfin
            """;

        BackupService.ValidateCustomNetflixDumpToc(Toc, Toc, "custom schema");
    }

    [Fact]
    public void ValidateCustomNetflixDumpToc_AcceptsSchemaMatchingObjectTypeText()
    {
        const string Toc =
            """
            ;     Format: CUSTOM
            10; 1259 100 TABLE CONSTRAINT cnx_profiles jellyfin
            11; 2606 101 FK CONSTRAINT CONSTRAINT cnx_profiles cnx_profiles_profile_fk jellyfin
            """;

        BackupService.ValidateCustomNetflixDumpToc(Toc, Toc, "CONSTRAINT");
    }

    [Fact]
    public void ValidateCustomNetflixDumpToc_RejectsOutOfScopeObjects()
    {
        const string ScopedToc =
            """
            ;     Format: CUSTOM
            10; 1259 100 TABLE public cnx_profiles jellyfin
            """;
        const string CompleteToc =
            """
            ;     Format: CUSTOM
            10; 1259 100 TABLE public cnx_profiles jellyfin
            20; 1259 200 TABLE audit payments jellyfin
            """;

        Assert.Throws<InvalidDataException>(
            () => BackupService.ValidateCustomNetflixDumpToc(CompleteToc, ScopedToc, "public"));
    }

    [Fact]
    public void ValidateCustomNetflixDumpToc_RejectsNonCnxObjectInDeclaredSchema()
    {
        const string Toc =
            """
            ;     Format: CUSTOM
            10; 1259 100 TABLE public evil_cnx_shadow jellyfin
            """;

        Assert.Throws<InvalidDataException>(
            () => BackupService.ValidateCustomNetflixDumpToc(Toc, Toc, "public"));
    }

    [Fact]
    public void ValidateCustomNetflixDumpToc_RejectsNonCustomFormatAndUnsafeSchema()
    {
        const string TarToc =
            """
            ;     Format: TAR
            ;     dbname: example Format: CUSTOM
            10; 1259 100 TABLE public cnx_profiles jellyfin
            """;

        Assert.Throws<InvalidDataException>(
            () => BackupService.ValidateCustomNetflixDumpToc(TarToc, TarToc, "public"));
        Assert.Throws<ArgumentException>(
            () => BackupService.ValidateCustomNetflixDumpToc(TarToc, TarToc, "public\nother"));
    }

    [Fact]
    public void ValidateCustomNetflixTargetSchema_RejectsMismatch()
    {
        BackupService.ValidateCustomNetflixTargetSchema("custom.schema", "custom.schema");

        Assert.Throws<InvalidOperationException>(
            () => BackupService.ValidateCustomNetflixTargetSchema("custom.schema", "public"));
    }

    [Fact]
    public void CalculateArchiveExpandedSize_RejectsOverflow()
    {
        Assert.Equal(6, BackupService.CalculateArchiveExpandedSize([1, 2, 3]));
        Assert.Throws<InvalidDataException>(
            () => BackupService.CalculateArchiveExpandedSize([long.MaxValue, 1]));
    }

    [Fact]
    public void CalculateRestoreTempRequirement_IncludesRollbackDumpAndSafetyMargin()
    {
        const long MinimumSafetyMargin = 64L * 1024 * 1024;

        Assert.Equal(
            MinimumSafetyMargin + 600,
            BackupService.CalculateRestoreTempRequirement(100, 200, 300));

        const long LargePayload = MinimumSafetyMargin * 20;
        Assert.Equal(
            LargePayload + (LargePayload / 10),
            BackupService.CalculateRestoreTempRequirement(LargePayload, 0, 0));

        Assert.Throws<InvalidDataException>(
            () => BackupService.CalculateRestoreTempRequirement(-1, 0, 0));
        Assert.Throws<InvalidDataException>(
            () => BackupService.CalculateRestoreTempRequirement(long.MaxValue, 1, 0));
    }

    [Fact]
    public void CalculateExistingFileRollbackSize_CountsOnlyFilesThatWillBeJournaled()
    {
        var testDirectory = CreateTestDirectory();
        try
        {
            var paths = CreateApplicationPaths(testDirectory);
            var sharedTargetDirectory = Path.Combine(testDirectory, "shared");
            paths.SetupGet(value => value.ConfigurationDirectoryPath).Returns(sharedTargetDirectory);
            paths.SetupGet(value => value.RootFolderPath).Returns(sharedTargetDirectory);
            var service = CreateService(
                paths,
                new Mock<IJellyfinDatabaseProvider>(),
                new Mock<IDbContextFactory<JellyfinDbContext>>());
            var stagingPath = Path.Combine(testDirectory, "staging");

            var configSourcePath = Path.Combine(stagingPath, "Config", "shared.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(configSourcePath)!);
            File.WriteAllBytes(configSourcePath, [1]);
            var rootSourcePath = Path.Combine(stagingPath, "Root", "shared.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(rootSourcePath)!);
            File.WriteAllBytes(rootSourcePath, [2]);
            var sharedTargetPath = Path.Combine(sharedTargetDirectory, "shared.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(sharedTargetPath)!);
            File.WriteAllBytes(sharedTargetPath, new byte[11]);

            var metadataSourcePath = Path.Combine(stagingPath, "Data", "metadata", "artwork.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(metadataSourcePath)!);
            File.WriteAllBytes(metadataSourcePath, [3]);
            var excludedDataTargetPath = Path.Combine(paths.Object.DataPath, "metadata", "artwork.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(excludedDataTargetPath)!);
            File.WriteAllBytes(excludedDataTargetPath, new byte[13]);
            var metadataTargetPath = Path.Combine(paths.Object.InternalMetadataPath, "artwork.bin");
            Directory.CreateDirectory(Path.GetDirectoryName(metadataTargetPath)!);
            File.WriteAllBytes(metadataTargetPath, new byte[17]);

            var newSourcePath = Path.Combine(stagingPath, "Config", "new.bin");
            File.WriteAllBytes(newSourcePath, [4]);

            Assert.Equal(28, service.CalculateExistingFileRollbackSize(stagingPath));
        }
        finally
        {
            Directory.Delete(testDirectory, true);
        }
    }

    [Fact]
    public void BuildBackupFileName_SameTimestampUsesUniqueSuffix()
    {
        var timestamp = new DateTimeOffset(2026, 1, 1, 3, 0, 0, TimeSpan.Zero);

        var first = BackupService.BuildBackupFileName(timestamp, Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var second = BackupService.BuildBackupFileName(timestamp, Guid.Parse("22222222-2222-2222-2222-222222222222"));

        Assert.NotEqual(first, second);
        Assert.StartsWith("jellyfin-backup-", first, StringComparison.Ordinal);
        Assert.EndsWith(".zip", first, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FlushCustomNetflixBuffersAsync_FlushesBothRegisteredBuffers()
    {
        var progressBuffer = new Mock<ICustomNetflixWatchProgressBuffer>();
        progressBuffer
            .Setup(value => value.FlushAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var eventBuffer = new Mock<ICustomNetflixWatchEventBuffer>();
        eventBuffer
            .Setup(value => value.FlushAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        using var serviceProvider = new ServiceCollection()
            .AddSingleton(progressBuffer.Object)
            .AddSingleton(eventBuffer.Object)
            .BuildServiceProvider();
        var service = CreateService(
            new Mock<IServerApplicationPaths>(),
            new Mock<IJellyfinDatabaseProvider>(),
            new Mock<IDbContextFactory<JellyfinDbContext>>(),
            serviceProvider: serviceProvider);

        await service.FlushCustomNetflixBuffersAsync();

        progressBuffer.Verify(
            value => value.FlushAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        eventBuffer.Verify(
            value => value.FlushAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static string CreateTestDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "jellyfin-full-backup-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static Mock<IServerApplicationPaths> CreateApplicationPaths(string testDirectory)
    {
        var paths = new Mock<IServerApplicationPaths>();
        paths.SetupGet(value => value.TempDirectory).Returns(Path.Combine(testDirectory, "temp"));
        paths.SetupGet(value => value.DataPath).Returns(Path.Combine(testDirectory, "data"));
        paths.SetupGet(value => value.CachePath).Returns(Path.Combine(testDirectory, "cache"));
        paths.SetupGet(value => value.ProgramDataPath).Returns(Path.Combine(testDirectory, "program"));
        paths.SetupGet(value => value.ConfigurationDirectoryPath).Returns(Path.Combine(testDirectory, "config"));
        paths.SetupGet(value => value.RootFolderPath).Returns(Path.Combine(testDirectory, "root"));
        paths.SetupGet(value => value.InternalMetadataPath).Returns(Path.Combine(testDirectory, "metadata"));
        paths.SetupGet(value => value.DefaultInternalMetadataPath).Returns(Path.Combine(testDirectory, "metadata-default"));
        return paths;
    }

    private static BackupService CreateService(
        Mock<IServerApplicationPaths> paths,
        Mock<IJellyfinDatabaseProvider> databaseProvider,
        Mock<IDbContextFactory<JellyfinDbContext>> dbContextFactory,
        IConfiguration? configuration = null,
        IServiceProvider? serviceProvider = null)
    {
        var applicationHost = new Mock<IServerApplicationHost>();
        applicationHost.SetupGet(host => host.ApplicationVersion).Returns(new Version(10, 12, 0));
        return new BackupService(
            NullLogger<BackupService>.Instance,
            dbContextFactory.Object,
            applicationHost.Object,
            paths.Object,
            databaseProvider.Object,
            Mock.Of<IHostApplicationLifetime>(),
            configuration ?? new ConfigurationBuilder().Build(),
            serviceProvider);
    }

    private static void CreateArchive(
        string archivePath,
        string historyJson,
        bool includeConfig,
        bool omitFirstTable = false,
        string configXml = "<ServerConfiguration />",
        bool customNetflixDatabase = false,
        bool includeCustomNetflixDump = false,
        bool legacyDatabaseTableManifest = false)
    {
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        var customNetflixValue = customNetflixDatabase ? "true" : "false";
        var entityTypes = typeof(JellyfinDbContext)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(entityType => entityType.PropertyType.IsAssignableTo(typeof(System.Linq.IQueryable)))
            .ToArray();
        string[] databaseTables = legacyDatabaseTableManifest
            ? [nameof(HistoryRow)]
            : entityTypes.Select(entityType => entityType.Name).Append(nameof(HistoryRow)).ToArray();
        var databaseTablesJson = JsonSerializer.Serialize(databaseTables);
        WriteEntry(
            archive,
            "manifest.json",
            $$"""
            {
              "ServerVersion": "10.12.0",
              "BackupEngineVersion": "0.2.0",
              "DateCreated": "2026-01-01T00:00:00Z",
              "DatabaseTables": {{databaseTablesJson}},
              "Options": {
                "Metadata": false,
                "Trickplay": false,
                "Subtitles": false,
                "Database": true,
                "CustomNetflixDatabase": {{customNetflixValue}}
              }
            }
            """);
        WriteEntry(archive, "Database/HistoryRow.json", historyJson);
        var omitted = false;
        foreach (var entityType in entityTypes)
        {
            if (omitFirstTable && !omitted)
            {
                omitted = true;
                continue;
            }

            WriteEntry(archive, $"Database/{entityType.Name}.json", "[]");
        }

        if (includeCustomNetflixDump)
        {
            WriteEntry(archive, "Database/customnetflix.pgdump", "not a valid PostgreSQL dump");
        }

        if (includeConfig)
        {
            WriteEntry(archive, "Config/system.xml", configXml);
        }
    }

    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        using var stream = archive.CreateEntry(path).Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes);
    }
}
