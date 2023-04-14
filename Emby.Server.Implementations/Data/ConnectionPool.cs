#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data;

public sealed class ConnectionPool : IDisposable
{
    private readonly BlockingCollection<SQLiteDatabaseConnection> _connections = new();
    private bool _disposed;

    public ConnectionPool(int count, Func<SQLiteDatabaseConnection> factory)
    {
        for (int i = 0; i < count; i++)
        {
            _connections.Add(factory.Invoke());
        }
    }

    public ManagedConnection GetConnection()
    {
        if (_disposed)
        {
            ThrowObjectDisposedException();
        }

        return new ManagedConnection(_connections.Take(), this);

        void ThrowObjectDisposedException()
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    public void Return(SQLiteDatabaseConnection connection)
    {
        if (_disposed)
        {
            connection.Dispose();
            return;
        }

        _connections.Add(connection);
    }

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

        _disposed = true;
    }
}
