using System;
using System.Collections.Concurrent;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data;

/// <summary>
/// A pool of SQLite Database connections.
/// </summary>
public sealed class ConnectionPool : IDisposable
{
    private readonly BlockingCollection<SQLiteDatabaseConnection> _connections = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionPool" /> class.
    /// </summary>
    /// <param name="count">The number of database connection to create.</param>
    /// <param name="factory">Factory function to create the database connections.</param>
    public ConnectionPool(int count, Func<SQLiteDatabaseConnection> factory)
    {
        for (int i = 0; i < count; i++)
        {
            _connections.Add(factory.Invoke());
        }
    }

    /// <summary>
    /// Gets a database connection from the pool if one is available, otherwise blocks.
    /// </summary>
    /// <returns>A database connection.</returns>
    public ManagedConnection GetConnection()
    {
        if (_disposed)
        {
            ThrowObjectDisposedException();
        }

        return new ManagedConnection(_connections.Take(), this);

        static void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(nameof(ConnectionPool));
        }
    }

    /// <summary>
    /// Return a database connection to the pool.
    /// </summary>
    /// <param name="connection">The database connection to return.</param>
    public void Return(SQLiteDatabaseConnection connection)
    {
        if (_disposed)
        {
            connection.Dispose();
            return;
        }

        _connections.Add(connection);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (var connection in _connections)
        {
            connection.Dispose();
        }

        _connections.Dispose();

        _disposed = true;
    }
}
