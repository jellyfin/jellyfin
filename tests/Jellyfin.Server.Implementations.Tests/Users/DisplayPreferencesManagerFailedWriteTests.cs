using System;
using System.Collections.Generic;
using System.Data.Common;
using Jellyfin.Server.Implementations.Tests.Item;
using Jellyfin.Server.Implementations.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users;

public sealed class DisplayPreferencesManagerFailedWriteTests : SqliteDbTestFixture
{
    private const string Client = "client";

    private static readonly Guid _userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _itemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly StatementInterceptor _statements;
    private readonly DisplayPreferencesManager _manager;

    public DisplayPreferencesManagerFailedWriteTests()
        : this(new StatementInterceptor())
    {
    }

    private DisplayPreferencesManagerFailedWriteTests(StatementInterceptor statements)
        : base(statements)
    {
        _statements = statements;
        _manager = new DisplayPreferencesManager(CreateDbContextFactory());
    }

    [Fact]
    public void SetCustomItemDisplayPreferences_InsertFails_KeepsThePreviousPreferences()
    {
        _manager.SetCustomItemDisplayPreferences(_userId, _itemId, Client, new Dictionary<string, string?> { ["first"] = "1" });

        _statements.FailInserts = true;
        Assert.Throws<DbUpdateException>(() => _manager.SetCustomItemDisplayPreferences(_userId, _itemId, Client, new Dictionary<string, string?> { ["second"] = "2" }));

        Assert.Equal(
            new Dictionary<string, string?> { ["first"] = "1" },
            _manager.ListCustomItemDisplayPreferences(_userId, _itemId, Client));
    }

    [Fact]
    public void SetCustomItemDisplayPreferences_AnotherRequestStoredAKeyAfterTheDelete_StoresThePreferencesItWasGiven()
    {
        _manager.SetCustomItemDisplayPreferences(_userId, _itemId, Client, new Dictionary<string, string?> { ["first"] = "1" });

        // The delete misses the row, as it does when another request stores it after this one's delete, so the unique
        // index rejects this request's insert.
        _statements.SkipNextDelete = true;
        _manager.SetCustomItemDisplayPreferences(_userId, _itemId, Client, new Dictionary<string, string?> { ["first"] = "2" });

        Assert.Equal(
            new Dictionary<string, string?> { ["first"] = "2" },
            _manager.ListCustomItemDisplayPreferences(_userId, _itemId, Client));
    }

    /// <summary>
    /// Fails inserts into CustomItemDisplayPreferences with the error SQLite reports when the unique index rejects a row, or
    /// leaves out the next delete from it.
    /// </summary>
    private sealed class StatementInterceptor : DbCommandInterceptor
    {
        public bool FailInserts { get; set; }

        public bool SkipNextDelete { get; set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            FailInsert(command);
            return result;
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            if (SkipNextDelete && command.CommandText.StartsWith("DELETE FROM \"CustomItemDisplayPreferences\"", StringComparison.Ordinal))
            {
                SkipNextDelete = false;
                return InterceptionResult<int>.SuppressWithResult(0);
            }

            FailInsert(command);
            return result;
        }

        private void FailInsert(DbCommand command)
        {
            if (FailInserts && command.CommandText.StartsWith("INSERT INTO \"CustomItemDisplayPreferences\"", StringComparison.Ordinal))
            {
                throw new SqliteException("SQLite Error 19: 'UNIQUE constraint failed: CustomItemDisplayPreferences.UserId, CustomItemDisplayPreferences.ItemId, CustomItemDisplayPreferences.Client, CustomItemDisplayPreferences.Key'.", 19);
            }
        }
    }
}
