#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Threading;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data;

public sealed class ConnectionPool : IDisposable
{
    private readonly int _count;
    private readonly SemaphoreSlim _lock;
    private readonly ConcurrentQueue<SQLiteDatabaseConnection> _connections = new ConcurrentQueue<SQLiteDatabaseConnection>();
    private bool _disposed;

    public ConnectionPool(int count, Func<SQLiteDatabaseConnection> factory)
    {
        _count = count;
        _lock = new SemaphoreSlim(count, count);
        for (int i = 0; i < count; i++)
        {
            _connections.Enqueue(factory.Invoke());
        }
    }

    public ManagedConnection GetConnection()
    {
        _lock.Wait();
        if (!_connections.TryDequeue(out var connection))
        {
            _lock.Release();
            throw new InvalidOperationException();
        }

        return new ManagedConnection(connection, this);
    }

    public void Return(SQLiteDatabaseConnection connection)
    {
        _connections.Enqueue(connection);
        _lock.Release();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        for (int i = 0; i < _count; i++)
        {
            _lock.Wait();
            if (!_connections.TryDequeue(out var connection))
            {
                _lock.Release();
                throw new InvalidOperationException();
            }

            connection.Dispose();
        }

        _lock.Dispose();

        _disposed = true;
    }
}
