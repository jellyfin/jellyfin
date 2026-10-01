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

    private readonly InsertFailer _insertFailer;
    private readonly DisplayPreferencesManager _manager;

    public DisplayPreferencesManagerFailedWriteTests()
        : this(new InsertFailer())
    {
    }

    private DisplayPreferencesManagerFailedWriteTests(InsertFailer insertFailer)
        : base(insertFailer)
    {
        _insertFailer = insertFailer;
        _manager = new DisplayPreferencesManager(CreateDbContextFactory());
    }

    [Fact]
    public void SetCustomItemDisplayPreferences_InsertFails_KeepsThePreviousPreferences()
    {
        _manager.SetCustomItemDisplayPreferences(_userId, _itemId, Client, new Dictionary<string, string?> { ["first"] = "1" });

        _insertFailer.FailNextInsert = true;
        Assert.Throws<DbUpdateException>(() => _manager.SetCustomItemDisplayPreferences(_userId, _itemId, Client, new Dictionary<string, string?> { ["second"] = "2" }));

        Assert.Equal(
            new Dictionary<string, string?> { ["first"] = "1" },
            _manager.ListCustomItemDisplayPreferences(_userId, _itemId, Client));
    }

    /// <summary>
    /// Fails the next insert into CustomItemDisplayPreferences with the error SQLite reports when the unique index rejects a row.
    /// </summary>
    private sealed class InsertFailer : DbCommandInterceptor
    {
        public bool FailNextInsert { get; set; }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            FailInsert(command);
            return result;
        }

        public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
        {
            FailInsert(command);
            return result;
        }

        private void FailInsert(DbCommand command)
        {
            if (FailNextInsert && command.CommandText.StartsWith("INSERT INTO \"CustomItemDisplayPreferences\"", StringComparison.Ordinal))
            {
                FailNextInsert = false;
                throw new SqliteException("SQLite Error 19: 'UNIQUE constraint failed: CustomItemDisplayPreferences.UserId, CustomItemDisplayPreferences.ItemId, CustomItemDisplayPreferences.Client, CustomItemDisplayPreferences.Key'.", 19);
            }
        }
    }
}
