using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Hands out databases of one server, each to a single test instance at a time, for the engines that cannot copy a
/// database cheaply. A database is built once and then reused: when its lease ends, it is reset and waits for the
/// next test. The pool grows when every database of a schema source is leased, so a test never waits for another one,
/// and for each schema source it can only grow as far as tests run at the same time. A database whose schema was
/// changed by a test is dropped instead of being reused. Every database that was created is dropped when the pool is
/// disposed, which also happens when the process exits.
/// </summary>
internal sealed class LeasedDatabasePool : IDisposable
{
    private readonly IPooledDatabaseServer _server;
    private readonly ConcurrentDictionary<string, byte> _databases = new();
    private readonly ConcurrentDictionary<string, string> _fingerprints = new();
    private readonly ConcurrentDictionary<TestSchemaSource, ConcurrentStack<string>> _idle = new();

    // Guards the switch to disposed against the registration of a new database, so that the sweep sees every database.
    private readonly Lock _registration = new();
    private volatile bool _disposed;

    public LeasedDatabasePool(IPooledDatabaseServer server)
    {
        _server = server;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    public TestDatabase Lease(TestSchemaSource source, IInterceptor[] interceptors, bool fresh)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (fresh)
        {
            var name = CreateDatabase(source);
            return Hand(name, interceptors, () => Drop(name));
        }

        var stack = _idle.GetOrAdd(source, _ => new ConcurrentStack<string>());
        var leased = stack.TryPop(out var idle) ? idle : CreateDatabase(source);
        return Hand(leased, interceptors, () => Return(leased, stack));
    }

    public TestDatabase CreateEmpty()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var name = CreateDatabase();
        return Hand(name, [], () => Drop(name));
    }

    public void Dispose()
    {
        string[] names;
        lock (_registration)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            names = [.. _databases.Keys];
        }

        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

        var failures = new List<string>();
        foreach (var name in names)
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
        lock (_registration)
        {
            if (!_disposed)
            {
                _databases[name] = 0;
                return name;
            }
        }

        // The pool was disposed after this lease began, so the sweep has not seen this database.
        _server.DropDatabase(name);
        throw new ObjectDisposedException(GetType().FullName);
    }

    private string CreateDatabase(TestSchemaSource source)
    {
        var name = CreateDatabase();
        try
        {
            source.Apply(_server.CreateOptions(name, []), _server.CreateProvider());
            _fingerprints[name] = _server.GetSchemaFingerprint(name);
        }
        catch (Exception failure)
        {
            DropAfterFailure(name, failure);
            throw;
        }

        return name;
    }

    private TestDatabase Hand(string name, IInterceptor[] interceptors, Action release)
    {
        var released = 0;
        return new TestDatabase(
            _server.CreateOptions(name, interceptors),
            _server.CreateProvider,
            () =>
            {
                if (Interlocked.Exchange(ref released, 1) == 0)
                {
                    release();
                }
            });
    }

    private void Return(string name, ConcurrentStack<string> idle)
    {
        if (_disposed)
        {
            Drop(name);
            return;
        }

        bool schemaChanged;
        try
        {
            // A reset only restores the contents of the tables of the model, so a test that changed more than that
            // leaves a database that no later test could rely on.
            schemaChanged = _fingerprints.TryGetValue(name, out var expected) && _server.GetSchemaFingerprint(name) != expected;
            if (!schemaChanged)
            {
                // The interceptors of the test that is done must not see the reset.
                DatabaseReset.Run(_server.CreateOptions(name, []), _server.CreateProvider());
            }
        }
        catch (Exception resetFailure)
        {
            // A database in an unknown state is never leased again.
            DropAfterFailure(name, resetFailure);

            // A reset that fails because the sweep dropped the database underneath it is expected at the end of the process.
            if (_disposed)
            {
                return;
            }

            throw;
        }

        if (schemaChanged)
        {
            Drop(name);
            return;
        }

        idle.Push(name);
    }

    private void DropAfterFailure(string name, Exception failure)
    {
        try
        {
            Drop(name);
        }
        catch (Exception dropFailure)
        {
            // The sweep on disposal tries to drop it again.
            throw new AggregateException(failure, dropFailure);
        }
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
        _fingerprints.TryRemove(name, out _);
    }
}
