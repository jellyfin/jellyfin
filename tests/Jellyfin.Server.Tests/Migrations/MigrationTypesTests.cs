// Disable StyleCop and CA analyzers for this test file - cosmetic/style rules don't apply to tests
#pragma warning disable SA1124 // Do not use regions
#pragma warning disable SA1502 // Element should not be on a single line
#pragma warning disable SA1201 // A method should not follow a class
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1516 // Elements should be separated by blank line
#pragma warning disable SA1649 // File name should match first type name
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1116 // Parameters should begin on next line
#pragma warning disable CA1305 // The behavior of DateTime.ToString could vary
#pragma warning disable CA1307 // StringComparison parameter
#pragma warning disable CA1860 // Prefer comparing Count to 0
#pragma warning disable xUnit2013 // Do not use Assert.Equal to check for collection size
#pragma warning disable xUnit2017 // Do not use Assert.True to check if a value exists
#pragma warning disable xUnit2005 // Do not use Same on value type

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Migrations;
using Emby.Server.Implementations.Migrations.Stages;
using Jellyfin.Server.Migrations;
using Jellyfin.Server.ServerSetupApp;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Tests for the <see cref="MigrationStage"/> collection type.
/// </summary>
public class MigrationStageTests
{
    private static JellyfinMigrationAttribute CreateMigrationMetadata(DateTime order, string name)
        => new JellyfinMigrationAttribute(order.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), name);

    private static CodeMigration CreateMigration(
        Type migrationType,
        DateTime order,
        string name,
        JellyfinMigrationStageTypes stage = JellyfinMigrationStageTypes.CoreInitialisation)
    {
        var metadata = new JellyfinMigrationAttribute(order.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), name)
        {
            Stage = stage
        };
        return new CodeMigration(migrationType, metadata, null);
    }

    private sealed class DummyMigrationType { }

    #region Stage Property Tests

    [Fact]
    public void Stage_ReturnsCorrectValue_ForPreInitialisation()
    {
        var stage = new MigrationStage(JellyfinMigrationStageTypes.PreInitialisation);
        Assert.Equal(JellyfinMigrationStageTypes.PreInitialisation, stage.Stage);
    }

    [Fact]
    public void Stage_ReturnsCorrectValue_ForCoreInitialisation()
    {
        var stage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        Assert.Equal(JellyfinMigrationStageTypes.CoreInitialisation, stage.Stage);
    }

    [Fact]
    public void Stage_ReturnsCorrectValue_ForAppInitialisation()
    {
        var stage = new MigrationStage(JellyfinMigrationStageTypes.AppInitialisation);
        Assert.Equal(JellyfinMigrationStageTypes.AppInitialisation, stage.Stage);
    }

    #endregion

    #region ICollection Implementation Tests

    [Fact]
    public void Add_AddsItem_ToCollection()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        var migration = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "TestMigration");

        migrationStage.Add(migration);

        Assert.Single(migrationStage);
        Assert.Contains(migration, migrationStage);
    }

    [Fact]
    public void Add_MultipleItems_AllAreAccessible()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        var migration1 = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "Migration1");
        var migration2 = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-02-15", System.Globalization.CultureInfo.InvariantCulture), "Migration2");
        var migration3 = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-03-15", System.Globalization.CultureInfo.InvariantCulture), "Migration3");

        migrationStage.Add(migration1);
        migrationStage.Add(migration2);
        migrationStage.Add(migration3);

        Assert.Equal(3, migrationStage.Count);
        Assert.Contains(migration1, migrationStage);
        Assert.Contains(migration2, migrationStage);
        Assert.Contains(migration3, migrationStage);
    }

    [Fact]
    public void Remove_RemovesExistingItem_AndReturnsTrue()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        var migration = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "TestMigration");
        migrationStage.Add(migration);

        var result = migrationStage.Remove(migration);

        Assert.True(result);
        Assert.Empty(migrationStage);
    }

    [Fact]
    public void Remove_RemoveNonExistentItem_AndReturnsFalse()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        var migration = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "TestMigration");

        var result = migrationStage.Remove(migration);

        Assert.False(result);
        Assert.Empty(migrationStage);
    }

    [Fact]
    public void Contains_ReturnsTrue_ForItemInCollection()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        var migration = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "TestMigration");
        migrationStage.Add(migration);

        Assert.True(migrationStage.Contains(migration));
    }

    [Fact]
    public void Contains_ReturnsFalse_ForItemNotInCollection()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        var migration = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "TestMigration");

        Assert.False(migrationStage.Contains(migration));
    }

    [Fact]
    public void Count_IsAccurate_AfterAdd()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);

        migrationStage.Add(CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "Migration1"));
        migrationStage.Add(CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-02-15", System.Globalization.CultureInfo.InvariantCulture), "Migration2"));
        migrationStage.Add(CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-03-15", System.Globalization.CultureInfo.InvariantCulture), "Migration3"));

        Assert.Equal(3, migrationStage.Count);
    }

    [Fact]
    public void Count_IsAccurate_AfterRemove()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        var migration1 = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "Migration1");
        var migration2 = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-02-15", System.Globalization.CultureInfo.InvariantCulture), "Migration2");
        var migration3 = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-03-15", System.Globalization.CultureInfo.InvariantCulture), "Migration3");

        migrationStage.Add(migration1);
        migrationStage.Add(migration2);
        migrationStage.Add(migration3);
        migrationStage.Remove(migration2);

        Assert.Equal(2, migrationStage.Count);
    }

    [Fact]
    public void Count_IsZero_AfterClear()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        migrationStage.Add(CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "Migration1"));
        migrationStage.Add(CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-02-15", System.Globalization.CultureInfo.InvariantCulture), "Migration2"));

        migrationStage.Clear();

        Assert.Equal(0, migrationStage.Count);
    }

    [Fact]
    public void Clear_EmptiesCollection()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        migrationStage.Add(CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "Migration1"));
        migrationStage.Add(CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-02-15", System.Globalization.CultureInfo.InvariantCulture), "Migration2"));

        migrationStage.Clear();

        Assert.Empty(migrationStage);
        Assert.False(migrationStage.Any());
    }

    [Fact]
    public void GetEnumerator_YieldsAllItems_InInsertionOrder()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        var migration1 = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "First");
        var migration2 = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-02-15", System.Globalization.CultureInfo.InvariantCulture), "Second");
        var migration3 = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-03-15", System.Globalization.CultureInfo.InvariantCulture), "Third");

        migrationStage.Add(migration1);
        migrationStage.Add(migration2);
        migrationStage.Add(migration3);

        var items = migrationStage.ToList();

        Assert.Collection(items,
            item => Assert.Same(migration1, item),
            item => Assert.Same(migration2, item),
            item => Assert.Same(migration3, item));
    }

    [Fact]
    public void GetEnumerator_EmptyCollection_YieldsNothing()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        var items = migrationStage.ToList();

        Assert.Empty(items);
    }

    [Fact]
    public void IsReadOnly_ReturnsFalse()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);

        Assert.False(migrationStage.IsReadOnly);
    }

    [Fact]
    public void CopyTo_CopiesItemsToArray()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        var migration1 = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "Migration1");
        var migration2 = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-02-15", System.Globalization.CultureInfo.InvariantCulture), "Migration2");

        migrationStage.Add(migration1);
        migrationStage.Add(migration2);

        var array = new CodeMigration[migrationStage.Count + 2]; // extra space
        migrationStage.CopyTo(array, 2);

        Assert.Null(array[0]);
        Assert.Null(array[1]);
        Assert.Same(migration1, array[2]);
        Assert.Same(migration2, array[3]);
    }

    [Fact]
    public void CopyTo_HandlesArrayOffset()
    {
        var migrationStage = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);
        var migration = CreateMigration(typeof(DummyMigrationType), DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "Migration1");

        migrationStage.Add(migration);

        var array = new CodeMigration[5];
        migrationStage.CopyTo(array, 3);

        Assert.Null(array[0]);
        Assert.Null(array[1]);
        Assert.Null(array[2]);
        Assert.Same(migration, array[3]);
        Assert.Null(array[4]);
    }

    #endregion

    #region ICollection Interface Implementation

    [Fact]
    public void ImplementsICollectionOfCodeMigration()
    {
        ICollection<CodeMigration> collection = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);

        Assert.NotNull(collection);
    }

    [Fact]
    public void ImplementsIEnumerableOfCodeMigration()
    {
        IEnumerable<CodeMigration> enumerable = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);

        Assert.NotNull(enumerable);
    }

    [Fact]
    public void ImplementsIEnumerable()
    {
        IEnumerable enumerable = new MigrationStage(JellyfinMigrationStageTypes.CoreInitialisation);

        Assert.NotNull(enumerable);
    }

    #endregion
}

/// <summary>
/// Tests for the <see cref="CodeMigration"/> type.
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
public class CodeMigrationTests
{
    private static JellyfinMigrationAttribute CreateMigrationMetadata(DateTime order, string name)
        => new JellyfinMigrationAttribute(order.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), name);

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsMigrationType()
    {
        var metadata = CreateMigrationMetadata(DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "TestMigration");
        var migration = new CodeMigration(typeof(DummyAsyncMigration), metadata, null);

        Assert.Equal(typeof(DummyAsyncMigration), migration.MigrationType);
    }

    [Fact]
    public void Constructor_SetsMetadata()
    {
        var expectedName = "TestMigration";
        var expectedOrder = DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture);
        var metadata = CreateMigrationMetadata(expectedOrder, expectedName);
        var migration = new CodeMigration(typeof(DummyAsyncMigration), metadata, null);

        Assert.Same(metadata, migration.Metadata);
        Assert.Equal(expectedName, migration.Metadata.Name);
        Assert.Equal(expectedOrder, migration.Metadata.Order);
    }

    [Fact]
    public void Constructor_SetsBackupRequirements_WhenProvided()
    {
        var metadata = CreateMigrationMetadata(DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "TestMigration");
        var backupMetadata = new JellyfinMigrationBackupAttribute { JellyfinDb = true, Metadata = false };
        var migration = new CodeMigration(typeof(DummyAsyncMigration), metadata, backupMetadata);

        Assert.Same(backupMetadata, migration.BackupRequirements);
        Assert.True(migration.BackupRequirements!.JellyfinDb);
        Assert.False(migration.BackupRequirements.Metadata);
    }

    [Fact]
    public void Constructor_SetsBackupRequirements_Null_WhenNotProvided()
    {
        var metadata = CreateMigrationMetadata(DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "TestMigration");
        var migration = new CodeMigration(typeof(DummyAsyncMigration), metadata, null);

        Assert.Null(migration.BackupRequirements);
    }

    #endregion

    #region BuildCodeMigrationId Tests

    [Theory]
    [InlineData("2024-01-15T10:30:00", "MyMigration", "Code_20240115103000_MyMigration")]
    [InlineData("2024-06-01T00:00:00", "DatabaseCleanup", "Code_20240601000000_DatabaseCleanup")]
    [InlineData("2023-12-31T23:59:59", "YearEndMigration", "Code_20231231235959_YearEndMigration")]
    public void BuildCodeMigrationId_ReturnsCorrectFormat(string dateTimeStr, string migrationName, string expectedId)
    {
        var order = DateTime.Parse(dateTimeStr, System.Globalization.CultureInfo.InvariantCulture);
        var metadata = new JellyfinMigrationAttribute(order.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), migrationName);
        var migration = new CodeMigration(typeof(DummyAsyncMigration), metadata, null);

        var id = migration.BuildCodeMigrationId();

        Assert.Equal(expectedId, id);
    }

    [Fact]
    public void BuildCodeMigrationId_PrefixIsCodeUnderscore()
    {
        var metadata = CreateMigrationMetadata(DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "TestMigration");
        var migration = new CodeMigration(typeof(DummyAsyncMigration), metadata, null);

        var id = migration.BuildCodeMigrationId();

        Assert.StartsWith("Code_", id, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildCodeMigrationId_ContainsNameSuffix()
    {
        const string name = "UniqueMigrationName";
        var order = DateTime.Parse("2024-01-01", System.Globalization.CultureInfo.InvariantCulture);
        var metadata = new JellyfinMigrationAttribute(order.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), name);
        var migration = new CodeMigration(typeof(DummyAsyncMigration), metadata, null);

        var id = migration.BuildCodeMigrationId();

        Assert.EndsWith("_" + name, id, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildCodeMigrationId_DateFormatIsyyyyMMddHHmmss()
    {
        var order = DateTime.ParseExact("2024-03-14 08:05:02", "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
        var metadata = new JellyfinMigrationAttribute(order.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), "Test");
        var migration = new CodeMigration(typeof(DummyAsyncMigration), metadata, null);

        var id = migration.BuildCodeMigrationId();

        // Extract the date portion between "Code_" and "_"
        var datePart = id.Substring(5, 14); // Code_ + 14 chars + _Name
        Assert.Equal("20240314080502", datePart);
    }

    #endregion

    #region Perform Tests

    /// <summary>
    /// Minimal service provider for testing Perform().
    /// </summary>
    private sealed class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    /// <summary>
    /// Dummy async migration for testing.
    /// Uses a static flag so that the instance created by Perform() is observable.
    /// </summary>
    private sealed class DummyAsyncMigration : IAsyncMigrationRoutine
    {
        public static bool WasPerformed { get; set; }

        public Task PerformAsync(CancellationToken cancellationToken)
        {
            WasPerformed = true;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Dummy sync migration for testing.
    /// Uses a static flag so that the instance created by Perform() is observable.
    /// </summary>
    private sealed class DummySyncMigration : IMigrationRoutine
    {
        public static bool WasPerformed { get; set; }

        public void Perform()
        {
            WasPerformed = true;
        }
    }

    /// <summary>
    /// Hybrid migration that implements both interfaces.
    /// Uses static flags for observability across instances.
    /// </summary>
    private sealed class DummyHybridMigration : IAsyncMigrationRoutine, IMigrationRoutine
    {
        public static bool AsyncPerformed { get; set; }
        public static bool SyncPerformed { get; set; }

        public Task PerformAsync(CancellationToken cancellationToken)
        {
            AsyncPerformed = true;
            return Task.CompletedTask;
        }

        public void Perform()
        {
            SyncPerformed = true;
        }
    }

    /// <summary>
    /// Migration that captures the cancellation token passed to it.
    /// Uses a static field for cross-instance visibility.
    /// </summary>
    private sealed class CancellableMigration : IAsyncMigrationRoutine
    {
        public static CancellationToken? CapturedToken { get; set; }

        public Task PerformAsync(CancellationToken cancellationToken)
        {
            CapturedToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Perform_CallsAsyncMigrationRoutine_PerformAsync()
    {
        DummyAsyncMigration.WasPerformed = false;
        var metadata = CreateMigrationMetadata(DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "AsyncMigration");
        var codeMigration = new CodeMigration(typeof(DummyAsyncMigration), metadata, null);
        var logger = CreateLogger();
        var sp = new TestServiceProvider();

        await codeMigration.Perform(sp, logger, CancellationToken.None);

        Assert.True(DummyAsyncMigration.WasPerformed);
    }

    [Fact]
    public async Task Perform_IgnoresAsyncMigrationThatAlsoImplementsSyncInterface()
    {
        DummyHybridMigration.AsyncPerformed = false;
        DummyHybridMigration.SyncPerformed = false;
        var metadata = CreateMigrationMetadata(DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "HybridMigration");
        var codeMigration = new CodeMigration(typeof(DummyHybridMigration), metadata, null);
        var logger = CreateLogger();
        var sp = new TestServiceProvider();

        await codeMigration.Perform(sp, logger, CancellationToken.None);

        // IAsyncMigrationRoutine takes precedence, so PerformAsync should be called
        Assert.True(DummyHybridMigration.AsyncPerformed);
        Assert.False(DummyHybridMigration.SyncPerformed);
    }

    [Fact]
    public async Task Perform_CallsSyncMigrationRoutine_Perform()
    {
        DummySyncMigration.WasPerformed = false;
        var metadata = CreateMigrationMetadata(DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "SyncMigration");
        var codeMigration = new CodeMigration(typeof(DummySyncMigration), metadata, null);
        var logger = CreateLogger();
        var sp = new TestServiceProvider();

        await codeMigration.Perform(sp, logger, CancellationToken.None);

        Assert.True(DummySyncMigration.WasPerformed);
    }

    [Fact]
    public async Task Perform_ThrowsForUnknownMigrationType()
    {
        var metadata = CreateMigrationMetadata(DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "UnknownMigration");
        var codeMigration = new CodeMigration(typeof(object), metadata, null);
        var logger = CreateLogger();
        var sp = new TestServiceProvider();

        var exception = await Record.ExceptionAsync(() => codeMigration.Perform(sp, logger, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("does not implement IAsyncMigrationRoutine or IMigrationRoutine", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Perform_PassesCancellationToken_ToAsyncRoutine()
    {
        CancellableMigration.CapturedToken = null;
        var metadata = CreateMigrationMetadata(DateTime.Parse("2024-01-15", System.Globalization.CultureInfo.InvariantCulture), "CancellableMigration");
        var codeMigration = new CodeMigration(typeof(CancellableMigration), metadata, null);
        var logger = CreateLogger();
        var sp = new TestServiceProvider();

        var cts = new CancellationTokenSource();
        await codeMigration.Perform(sp, logger, cts.Token);

        Assert.Equal(cts.Token, CancellableMigration.CapturedToken);
    }

    #endregion

    #region Logger (minimal implementation for testing)

    /// <summary>
    /// Creates a mock IStartupLogger for testing.
    /// </summary>
    private static IStartupLogger CreateLogger()
    {
        var loggerMock = new Mock<IStartupLogger>();
        loggerMock.Setup(x => x.BeginGroup(It.IsAny<FormattableString>())).Returns(loggerMock.Object);
        loggerMock.Setup(x => x.With(It.IsAny<ILogger>())).Returns(loggerMock.Object);
        return loggerMock.Object;
    }

    #endregion
}
#pragma warning restore CS0618 // Type or member is obsolete
