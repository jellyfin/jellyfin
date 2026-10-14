using System;
using System.IO;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Migrations.Routines;
using MediaBrowser.Controller;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Covers the import of the legacy activity log database, in particular the entries that reference a
/// user by the integer id of an old schema instead of by Guid. The routine resets the identity counter
/// of the table through <c>sqlite_sequence</c>, so it only runs against SQLite.
/// </summary>
public sealed class MigrateActivityLogDbTests : DatabaseTestFixture
{
    private static readonly Guid _firstUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid _resolvedUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid _lastUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private readonly string _root;
    private readonly string _dataPath;

    public MigrateActivityLogDbTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "jf-activitylog-" + Guid.NewGuid().ToString("N"));
        _dataPath = Path.Combine(_root, "data");
        Directory.CreateDirectory(_dataPath);
    }

    [ProviderTheory(TestDbProvider.Sqlite)]
    [InlineData(false)]
    [InlineData(true)]
    public void Perform_LegacyUserIdFoundInUserDatabase_ImportsEveryEntryWithResolvedUser(bool guidsAsText)
    {
        CreateActivityLogDb(guidsAsText);
        CreateUserDb(userId: _resolvedUserId, guidAsText: guidsAsText);

        Perform();

        using var context = CreateDbContext();
        var logs = context.ActivityLogs.OrderBy(l => l.Id).ToList();
        Assert.Equal(["first", "second", "third"], logs.Select(l => l.Name));
        Assert.Equal([_firstUserId, _resolvedUserId, _lastUserId], logs.Select(l => l.UserId));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void Perform_LegacyUserIdMissingFromUserDatabase_ImportsEveryEntryWithEmptyUser()
    {
        CreateActivityLogDb(guidsAsText: false);
        CreateUserDb(userId: null);

        Perform();

        using var context = CreateDbContext();
        var logs = context.ActivityLogs.OrderBy(l => l.Id).ToList();
        Assert.Equal(["first", "second", "third"], logs.Select(l => l.Name));
        Assert.Equal([_firstUserId, Guid.Empty, _lastUserId], logs.Select(l => l.UserId));
    }

    [ProviderFact(TestDbProvider.Sqlite)]
    public void Perform_PreviousImportRowsWereRemoved_RestartsIdsAtOne()
    {
        SeedActivityLogs(5);
        CreateActivityLogDb(
            ("first", _firstUserId.ToByteArray()),
            ("second", _lastUserId.ToByteArray()));
        CreateUserDb(userId: null);

        Perform();

        using var context = CreateDbContext();
        var logs = context.ActivityLogs.OrderBy(l => l.Id).ToList();
        Assert.Equal([1, 2], logs.Select(l => l.Id));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        base.Dispose(disposing);
    }

    private SqliteConnection OpenLegacyDb(string fileName)
    {
#pragma warning disable RS0030 // Do not use banned APIs: The file to import is a legacy SQLite database whichever provider is selected.
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path.Combine(_dataPath, fileName), Pooling = false }.ToString());
#pragma warning restore RS0030
        connection.Open();
        return connection;
    }

    private void CreateActivityLogDb(bool guidsAsText)
        => CreateActivityLogDb(
            ("first", LegacyGuid(_firstUserId, guidsAsText)),
            ("second", 1L),
            ("third", LegacyGuid(_lastUserId, guidsAsText)));

    private void CreateActivityLogDb(params (string Name, object UserId)[] entries)
    {
        using var connection = OpenLegacyDb("activitylog.db");
        using var create = connection.CreateCommand();
        create.CommandText = """
            CREATE TABLE ActivityLog (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Overview TEXT,
                ShortOverview TEXT,
                Type TEXT NOT NULL,
                ItemId TEXT,
                UserId GUID,
                DateCreated DATETIME NOT NULL,
                LogSeverity TEXT NOT NULL)
            """;
        create.ExecuteNonQuery();

        foreach (var entry in entries)
        {
            InsertEntry(connection, entry.Name, entry.UserId);
        }
    }

    private static object LegacyGuid(Guid guid, bool asText)
        => asText ? guid.ToString() : guid.ToByteArray();

    private void SeedActivityLogs(int count)
    {
        using var context = CreateDbContext();
        for (var i = 0; i < count; i++)
        {
            context.ActivityLogs.Add(new ActivityLog($"old {i}", "Old", Guid.Empty));
        }

        context.SaveChanges();
    }

    private static void InsertEntry(SqliteConnection connection, string name, object userId)
    {
        using var insert = connection.CreateCommand();
        insert.CommandText = """
            INSERT INTO ActivityLog (Name, Type, UserId, DateCreated, LogSeverity)
            VALUES (@Name, 'UserLoggedIn', @UserId, '2025-01-01 10:00:00', 'Information')
            """;
        insert.Parameters.AddWithValue("@Name", name);
        insert.Parameters.AddWithValue("@UserId", userId);
        insert.ExecuteNonQuery();
    }

    private void CreateUserDb(Guid? userId, bool guidAsText = false)
    {
        using var connection = OpenLegacyDb("users.db");
        using var create = connection.CreateCommand();
        create.CommandText = "CREATE TABLE LocalUsersv2 (Id INTEGER PRIMARY KEY AUTOINCREMENT, guid GUID NOT NULL, data BLOB NOT NULL)";
        create.ExecuteNonQuery();

        if (userId is null)
        {
            return;
        }

        using var insert = connection.CreateCommand();
        insert.CommandText = "INSERT INTO LocalUsersv2 (Id, guid, data) VALUES (1, @Guid, x'00')";
        insert.Parameters.AddWithValue("@Guid", LegacyGuid(userId.Value, guidAsText));
        insert.ExecuteNonQuery();
    }

    private void Perform()
    {
        var appPaths = new Mock<IServerApplicationPaths>();
        appPaths.SetupGet(p => p.DataPath).Returns(_dataPath);

        new MigrateActivityLogDb(
            NullLogger<MigrateActivityLogDb>.Instance,
            appPaths.Object,
            CreateDbContextFactory())
            .Perform();
    }
}
