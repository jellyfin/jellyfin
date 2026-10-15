using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.FullSystemBackup;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.SystemBackupService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Database.Testing.Conformance;

/// <summary>
/// Kit row: rows inserted with explicit values in their identity column, as a restore inserts them, keep those values,
/// and the next row saved afterwards gets a value above all of them; and a backup of every table, restored after the
/// tables were changed, gives back exactly the rows that were backed up, with two named exceptions that the row checks
/// are still needed.
/// </summary>
public abstract class RestoreConformance : ConformanceTestBase
{
    /// <summary>
    /// The tables a backup does not carry, so that a restore leaves them empty. The backup does not carry HomeSection,
    /// which has no set on <see cref="JellyfinDbContext"/>, and the purge of DisplayPreferences deletes its rows in
    /// cascade. A table here fails the row once the restore gives its rows back; remove it from the list then.
    /// </summary>
    /// <remarks>
    /// The backup does not carry the concurrency tokens either: they are read back as 0 after a restore. They are left
    /// out of the comparison, and the row fails once a restore keeps them.
    /// </remarks>
    private static readonly Type[] _notInTheBackup = [typeof(HomeSection)];

    private readonly string _root = Path.Combine(Path.GetTempPath(), "jellyfin-conformance-" + Guid.NewGuid().ToString("N"));

    [ConformanceFact]
    public async Task ExplicitIdentityInsert_EveryIdentityTable_KeepsTheValuesAndHandsOutHigherOnes()
    {
        long[] explicitIds = [7, 42];
        var tables = TablesWithIdentity();

        // The rows the new rows point at are saved before the transaction, as a restore has them in the archive.
        var principals = tables.ToDictionary(table => table.Name, table => explicitIds.Select(_ => Rows.PrincipalValues(table)).ToList(), StringComparer.Ordinal);

        await using (var context = CreateDbContext())
        {
            // As in a restore: one transaction, one table at a time. The tables keep the rows their principals made,
            // whose identities are below the explicit ones.
            await using var transaction = await context.Database.BeginTransactionAsync(CancellationToken);
            foreach (var table in tables)
            {
                var identity = ModelRowFactory.IdentityKey(table)!;
                var scope = await DatabaseProvider.BeginExplicitIdentityInsertAsync(context, context.Model.FindEntityType(table.Name)!, CancellationToken);
                await using (scope)
                {
                    for (var i = 0; i < explicitIds.Length; i++)
                    {
                        var values = new Dictionary<string, object?>(principals[table.Name][i], StringComparer.Ordinal)
                        {
                            [identity.Name] = Convert.ChangeType(explicitIds[i], identity.ClrType, CultureInfo.InvariantCulture)
                        };
                        Rows.Add(context, table, values);
                    }

                    await context.SaveChangesAsync(CancellationToken);
                    context.ChangeTracker.Clear();
                }
            }

            await DatabaseProvider.CompleteDatabaseRestoreAsync(context, CancellationToken);
            await transaction.CommitAsync(CancellationToken);
        }

        var wrong = new List<string>();
        foreach (var table in tables)
        {
            var stored = IdentityValues(table);
            var next = NextIdentity(table);
            if (!explicitIds.All(stored.Contains) || next <= explicitIds.Max())
            {
                wrong.Add($"{table.GetTableName()}: stored {string.Join(", ", stored)}, next {next}");
            }
        }

        AssertNone(wrong);
    }

    [ConformanceFact]
    public async Task RestoreBackup_EveryTable_GivesBackTheRowsThatWereBackedUp()
    {
        var tables = Tables();
        foreach (var table in tables)
        {
            Rows.Save(table);
            Rows.Save(table);
        }

        var backedUp = tables.ToDictionary(table => table.Name, table => DescribeRows(table, IsNotAConcurrencyToken), StringComparer.Ordinal);
        var highest = TablesWithIdentity().ToDictionary(table => table.Name, table => IdentityValues(table).Max(), StringComparer.Ordinal);
        var archive = await CreateBackupService().CreateBackupAsync(new BackupOptionsDto());

        // Every table gets a row more, which the restore has to remove again.
        foreach (var table in tables)
        {
            Rows.Save(table);
        }

        await CreateBackupService().RestoreBackupAsync(archive.Path);

        var wrong = new List<string>();
        foreach (var table in tables)
        {
            var restored = DescribeRows(table, IsNotAConcurrencyToken);
            if (_notInTheBackup.Contains(table.ClrType))
            {
                if (restored.Count > 0)
                {
                    wrong.Add($"{table.GetTableName()}: the restore now gives rows back, remove the table from the tables the backup does not carry");
                }
            }
            else if (!backedUp[table.Name].SequenceEqual(restored))
            {
                wrong.Add($"{table.GetTableName()}: lost [{string.Join(" | ", backedUp[table.Name].Except(restored))}], gained [{string.Join(" | ", restored.Except(backedUp[table.Name]))}]");
            }

            foreach (var token in table.GetProperties().Where(property => property.IsConcurrencyToken))
            {
                if (DescribeRows(table, property => property == token).Any(row => !string.Equals(row, $"{token.Name}=0", StringComparison.Ordinal)))
                {
                    wrong.Add($"{table.GetTableName()}.{token.Name}: the restore now keeps the concurrency token, compare it with the other columns");
                }
            }
        }

        foreach (var table in TablesWithIdentity().Where(table => !_notInTheBackup.Contains(table.ClrType)))
        {
            var next = NextIdentity(table);
            if (next <= highest[table.Name])
            {
                wrong.Add($"{table.GetTableName()}: next {next}, highest restored {highest[table.Name]}");
            }
        }

        AssertNone(wrong);
    }

    private static bool IsNotAConcurrencyToken(IProperty property) => !property.IsConcurrencyToken;

    protected override void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_root))
        {
            Directory.Delete(_root, true);
        }

        base.Dispose(disposing);
    }

    private BackupService CreateBackupService()
    {
        var applicationHost = new Mock<IServerApplicationHost>();
        applicationHost.Setup(a => a.ApplicationVersion).Returns(new Version(10, 11, 0));

        var applicationPaths = new Mock<IServerApplicationPaths>();
        string Directory(string name)
        {
            var path = Path.Combine(_root, name);
            System.IO.Directory.CreateDirectory(path);
            return path;
        }

        applicationPaths.Setup(a => a.BackupPath).Returns(Directory("Backup"));
        applicationPaths.Setup(a => a.ConfigurationDirectoryPath).Returns(Directory("Config"));
        applicationPaths.Setup(a => a.DataPath).Returns(Directory("Data"));
        applicationPaths.Setup(a => a.CachePath).Returns(Directory("Cache"));
        applicationPaths.Setup(a => a.ProgramDataPath).Returns(Directory("ProgramData"));
        applicationPaths.Setup(a => a.RootFolderPath).Returns(Directory("Root"));
        applicationPaths.Setup(a => a.InternalMetadataPath).Returns(Directory("Metadata"));
        applicationPaths.Setup(a => a.DefaultInternalMetadataPath).Returns(Directory("MetadataDefault"));

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.IsScanRunning).Returns(false);

        var routineCatalog = new Mock<IMigrationRoutineCatalog>();
        routineCatalog.Setup(c => c.RoutineIds).Returns(new HashSet<string>(StringComparer.Ordinal));

        return new BackupService(
            NullLogger<BackupService>.Instance,
            CreateDbContextFactory(),
            applicationHost.Object,
            applicationPaths.Object,
            DatabaseProvider,
            new Mock<IHostApplicationLifetime>().Object,
            libraryManager.Object,
            routineCatalog.Object);
    }
}
