using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Jellyfin.Database.Testing;
using Jellyfin.Extensions.Json;
using Jellyfin.Server.Implementations;
using Jellyfin.Server.Migrations.Routines;
using MediaBrowser.Controller;
using MediaBrowser.Model.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// The legacy user database did not limit the length of a user name, so it can hold a name that is longer than the
/// column of the new database. The import skips such a user and keeps the others.
/// </summary>
public sealed class MigrateUserDbKeyLengthTests : DatabaseTestFixture
{
    private readonly string _dataPath = Path.Combine(AppContext.BaseDirectory, "jellyfin-user-db-tests-" + Guid.NewGuid().ToString("N"));
    private readonly CapturingLogger<MigrateUserDb> _log = new();

    public MigrateUserDbKeyLengthTests()
    {
        Directory.CreateDirectory(_dataPath);
    }

    [Fact]
    public void Perform_UserNameOverTheBound_SkipsThatUserAndImportsTheOthers()
    {
        var overlongName = new string('u', KeyNormalizer.UsernameMaxLength + 45);
        var overlongId = Guid.NewGuid();
        var normalId = Guid.NewGuid();
        CreateUserDb((1, overlongId, overlongName), (2, normalId, "normal"));

        CreateMigration().Perform();

        using var context = CreateDbContext();
        var user = Assert.Single(context.Users.AsNoTracking().ToList());
        Assert.Equal("normal", user.Username);
        Assert.Equal(normalId, user.Id);
        Assert.Equal(2, user.InternalId);
        var warning = Assert.Single(_log.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains(overlongId.ToString(), warning.Message, StringComparison.Ordinal);
        Assert.Contains(overlongName, warning.Message, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(_dataPath, "users.db.old")));
    }

    [Fact]
    public void Perform_UserNameAtTheBound_IsImported()
    {
        var name = new string('u', KeyNormalizer.UsernameMaxLength);
        CreateUserDb((1, Guid.NewGuid(), name));

        CreateMigration().Perform();

        using var context = CreateDbContext();
        Assert.Equal(name, Assert.Single(context.Users.AsNoTracking().ToList()).Username);
        Assert.DoesNotContain(_log.Entries, e => e.Level == LogLevel.Warning);
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

    private MigrateUserDb CreateMigration()
    {
        var paths = new Mock<IServerApplicationPaths>();
        paths.Setup(e => e.DataPath).Returns(_dataPath);
        paths.Setup(e => e.UserConfigurationDirectoryPath).Returns(Path.Combine(_dataPath, "users"));

        return new MigrateUserDb(_log, paths.Object, CreateDbContextFactory(), Mock.Of<IXmlSerializer>());
    }

    private void CreateUserDb(params (long InternalId, Guid Id, string Name)[] users)
    {
#pragma warning disable RS0030 // Do not use banned APIs: The file to import is the legacy SQLite user database whichever provider is selected.
        using var connection = new SqliteConnection($"Filename={Path.Combine(_dataPath, "users.db")};Pooling=False");
#pragma warning restore RS0030
        connection.Open();

        using (var create = connection.CreateCommand())
        {
            create.CommandText = "CREATE TABLE LocalUsersv2 (Id INTEGER PRIMARY KEY, guid TEXT NOT NULL, data BLOB NOT NULL)";
            create.ExecuteNonQuery();
        }

        foreach (var user in users)
        {
            using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO LocalUsersv2 (Id, guid, data) VALUES ($id, $guid, $data)";
            insert.Parameters.AddWithValue("$id", user.InternalId);
            insert.Parameters.AddWithValue("$guid", user.Id.ToString());
            insert.Parameters.AddWithValue(
                "$data",
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { user.Name, ImageInfos = Array.Empty<object>() }, JsonDefaults.Options)));
            insert.ExecuteNonQuery();
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
