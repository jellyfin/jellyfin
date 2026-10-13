using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Hands out databases of one server, each to a single test instance at a time, for the engines that cannot copy a
/// database cheaply. Every database that was created is dropped when the pool is disposed, which also happens when
/// the process exits.
/// </summary>
internal sealed class LeasedDatabasePool : IDisposable
{
    private readonly IPooledDatabaseServer _server;
    private readonly ConcurrentDictionary<string, byte> _databases = new();
    private int _disposed;

    public LeasedDatabasePool(IPooledDatabaseServer server)
    {
        _server = server;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    public TestDatabase Lease(TestSchemaSource source, IInterceptor[] interceptors)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var name = CreateDatabase();
        source.Apply(_server.CreateOptions(name, []), _server.CreateProvider());
        return Hand(name, interceptors);
    }

    public TestDatabase CreateEmpty()
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        return Hand(CreateDatabase(), []);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

        var failures = new List<string>();
        foreach (var name in _databases.Keys)
        {
            try
            {
                Drop(name);
            }
            catch (Exception ex)
            {
                failures.Add($"{name}: {ex.Message}");
            }
        }

        if (failures.Count > 0)
        {
            Console.Error.WriteLine($"Could not drop {failures.Count} test database(s):{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
        }

        try
        {
            _server.Dispose();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not release the test database server: {ex.Message}");
        }
    }

    private void OnProcessExit(object? sender, EventArgs e) => Dispose();

    private string CreateDatabase()
    {
        var name = _server.CreateDatabase();
        _databases[name] = 0;
        return name;
    }

    private TestDatabase Hand(string name, IInterceptor[] interceptors)
    {
        return new TestDatabase(_server.CreateOptions(name, interceptors), _server.CreateProvider, () => Drop(name));
    }

    private void Drop(string name)
    {
        // A database that was dropped already, for example by disposing the pool, is gone.
        if (!_databases.ContainsKey(name))
        {
            return;
        }

        _server.DropDatabase(name);
        _databases.TryRemove(name, out _);
    }
}
