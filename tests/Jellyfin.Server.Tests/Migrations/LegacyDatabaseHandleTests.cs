using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Migrations.Routines;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// The import of a legacy database renames the file afterwards. Windows refuses to rename a file that is still open, so
/// the import must not keep a handle to it.
/// </summary>
public sealed class LegacyDatabaseHandleTests : DatabaseTestFixture
{
    private const string ProcessHandles = "/proc/self/fd";

    private readonly string _dataPath = Path.Combine(AppContext.BaseDirectory, "jellyfin-legacy-db-handle-tests-" + Guid.NewGuid().ToString("N"));

    public LegacyDatabaseHandleTests()
    {
        Directory.CreateDirectory(_dataPath);
    }

    [Fact]
    public void Perform_UserDb_DoesNotKeepTheLegacyDatabaseOpen()
    {
        Assert.SkipUnless(Directory.Exists(ProcessHandles), "The open files of the process cannot be listed");
        CreateLegacyDb(
            "users.db",
            "CREATE TABLE LocalUsersv2 (Id INTEGER PRIMARY KEY, guid TEXT NOT NULL, data BLOB NOT NULL)",
            "INSERT INTO LocalUsersv2 (Id, guid, data) VALUES (1, '" + Guid.NewGuid() + "', x'" + Convert.ToHexString(Encoding.UTF8.GetBytes("""{"Name":"user","ImageInfos":[]}""")) + "')");

        new MigrateUserDb(NullLogger<MigrateUserDb>.Instance, CreatePaths(), CreateDbContextFactory(), Mock.Of<IXmlSerializer>()).Perform();

        Assert.Empty(OpenLegacyFiles());
        Assert.True(File.Exists(Path.Combine(_dataPath, "users.db.old")));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void Perform_ActivityLogDb_DoesNotKeepTheLegacyDatabasesOpen()
    {
        Assert.SkipUnless(Directory.Exists(ProcessHandles), "The open files of the process cannot be listed");
        CreateLegacyDb("activitylog.db", "CREATE TABLE ActivityLog (Id INTEGER PRIMARY KEY)");
        CreateLegacyDb("users.db", "CREATE TABLE LocalUsersv2 (Id INTEGER PRIMARY KEY, guid TEXT NOT NULL, data BLOB NOT NULL)");

        new MigrateActivityLogDb(NullLogger<MigrateActivityLogDb>.Instance, CreatePaths(), CreateDbContextFactory()).Perform();

        Assert.Empty(OpenLegacyFiles());
        Assert.True(File.Exists(Path.Combine(_dataPath, "activitylog.db.old")));
    }

    [Fact]
    public void Perform_AuthenticationDb_DoesNotKeepTheLegacyDatabaseOpen()
    {
        Assert.SkipUnless(Directory.Exists(ProcessHandles), "The open files of the process cannot be listed");
        CreateLegacyDb(
            "authentication.db",
            "CREATE TABLE Tokens (Id INTEGER PRIMARY KEY)",
            "CREATE TABLE Devices (Id INTEGER PRIMARY KEY)");

        new MigrateAuthenticationDb(NullLogger<MigrateAuthenticationDb>.Instance, CreateDbContextFactory(), CreatePaths(), Mock.Of<IUserManager>()).Perform();

        Assert.Empty(OpenLegacyFiles());
        Assert.True(File.Exists(Path.Combine(_dataPath, "authentication.db.old")));
    }

    [Fact]
    public void Perform_DisplayPreferencesDb_DoesNotKeepTheLegacyDatabaseOpen()
    {
        Assert.SkipUnless(Directory.Exists(ProcessHandles), "The open files of the process cannot be listed");
        CreateLegacyDb(
            "displaypreferences.db",
            "CREATE TABLE userdisplaypreferences (id INTEGER PRIMARY KEY, userId TEXT NOT NULL, client TEXT NOT NULL, data BLOB NOT NULL)",
            "INSERT INTO userdisplaypreferences (id, userId, client, data) VALUES (1, '" + Guid.NewGuid() + "', 'web', x'" + Convert.ToHexString(Encoding.UTF8.GetBytes("{}")) + "')");

        new MigrateDisplayPreferencesDb(NullLogger<MigrateDisplayPreferencesDb>.Instance, CreatePaths(), CreateDbContextFactory(), Mock.Of<IUserManager>()).Perform();

        Assert.Empty(OpenLegacyFiles());
        Assert.True(File.Exists(Path.Combine(_dataPath, "displaypreferences.db.old")));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_dataPath))
            {
                Directory.Delete(_dataPath, true);
            }
        }

        base.Dispose(disposing);
    }

    private IServerApplicationPaths CreatePaths()
    {
        var paths = new Mock<IServerApplicationPaths>();
        paths.Setup(e => e.DataPath).Returns(_dataPath);
        paths.Setup(e => e.UserConfigurationDirectoryPath).Returns(Path.Combine(_dataPath, "users"));
        return paths.Object;
    }

    private void CreateLegacyDb(string fileName, params string[] statements)
    {
#pragma warning disable RS0030 // Do not use banned APIs: The file to import is a legacy SQLite database whichever provider is selected.
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(_dataPath, fileName), Pooling = false }.ToString());
#pragma warning restore RS0030
        connection.Open();
        foreach (var statement in statements)
        {
            using var command = connection.CreateCommand();
#pragma warning disable CA2100 // The statements are fixed texts of the tests.
            command.CommandText = statement;
#pragma warning restore CA2100
            command.ExecuteNonQuery();
        }
    }

    private List<string> OpenLegacyFiles()
    {
        // The links of /proc are resolved paths, so the directory is matched by its unique name
        // and not by the path, which may contain a symbolic link. Tests run in parallel, so a
        // descriptor can be closed while the list is read, which skips that entry.
        var directoryName = Path.GetFileName(_dataPath);
        var openFiles = new List<string>();
        foreach (var handle in Directory.EnumerateFileSystemEntries(ProcessHandles))
        {
            string? target;
            try
            {
                target = new FileInfo(handle).LinkTarget;
            }
            catch (IOException)
            {
                continue;
            }

            if (target is not null && target.Split('/').Contains(directoryName, StringComparer.Ordinal))
            {
                openFiles.Add(Path.GetFileName(target));
            }
        }

        return openFiles;
    }
}
