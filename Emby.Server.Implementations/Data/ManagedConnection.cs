#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace Emby.Server.Implementations.Data;

public sealed class ManagedConnection : IDisposable
{
    private readonly SemaphoreSlim? _writeLock;

    private SqliteConnection _db;

    private bool _disposed = false;

    public ManagedConnection(SqliteConnection db, SemaphoreSlim? writeLock)
    {
        _db = db;
        _writeLock = writeLock;
    }

    public SqliteTransaction BeginTransaction()
        => _db.BeginTransaction();

    public SqliteCommand CreateCommand()
        => _db.CreateCommand();

    public void Execute(string commandText)
        => _db.Execute(commandText);

    public SqliteCommand PrepareStatement(string sql)
        => _db.PrepareStatement(sql);

    public IEnumerable<SqliteDataReader> Query(string commandText)
        => _db.Query(commandText);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_writeLock is null)
        {
            // Read connections are managed with an internal pool
            _db.Dispose();
        }
        else
        {
            // Write lock is managed by BaseSqliteRepository
            // Don't dispose here
            _writeLock.Release();
        }

        _db = null!;

        _disposed = true;
    }
}
