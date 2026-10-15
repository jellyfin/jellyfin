using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Hands out databases of one server, each to a single test instance at a time, for the engines that cannot copy a
/// database cheaply. A database is built once and then reused: when its lease ends, it is reset and waits for the
/// next test. The pool grows when every database of a schema source is leased, so a test never waits for another one,
/// and for each schema source it can only grow as far as tests run at the same time. A database whose schema or
/// migration history was changed by a test is dropped instead of being reused. Every database that was created is
/// dropped when the pool is disposed, which also happens when the process exits.
/// </summary>
/// <remarks>
/// The reset empties the tables of the model only, so it keeps the migration history. The pool compares the history
/// together with the structure that the server describes: a later test would otherwise start from the migrations that
/// an earlier one recorded or removed. This holds for every server, so no server has to remember to read the history.
/// </remarks>
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

    /// <summary>
    /// Gets or sets a callback run in the window between the first check of disposal and taking the lock to pick an idle
    /// database. It exists so that a test can dispose the pool exactly there, which no other test can do on purpose.
    /// </summary>
    internal Action? BeforeIdleDatabaseIsTaken { get; set; }

    public TestDatabase Lease(TestSchemaSource source, IInterceptor[] interceptors, bool fresh)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (fresh)
        {
            // A database that is dropped when the lease ends is never compared with its fingerprint.
            var name = CreateDatabase(source, recordFingerprint: false);
            return Hand(name, interceptors, () => Drop(name));
        }

        var stack = _idle.GetOrAdd(source, _ => new ConcurrentStack<string>());

        // The sweep on disposal drops the idle databases too, so one must not be taken once it has begun.
        BeforeIdleDatabaseIsTaken?.Invoke();
        string? idle;
        lock (_registration)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            stack.TryPop(out idle);
        }

        var leased = idle ?? CreateDatabase(source, recordFingerprint: true);
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

    private string CreateDatabase(TestSchemaSource source, bool recordFingerprint)
    {
        var name = CreateDatabase();
        try
        {
            source.Apply(_server.CreateOptions(name, []), _server.CreateProvider());
            if (recordFingerprint)
            {
                _fingerprints[name] = Fingerprint(name);
            }
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
            schemaChanged = _fingerprints.TryGetValue(name, out var expected) && Fingerprint(name) != expected;
            if (!schemaChanged)
            {
                // The interceptors of the test that is done must not see the reset.
                DatabaseReset.Run(_server.CreateOptions(name, []), _server.CreateProvider());
            }
        }
        catch (Exception resetFailure)
        {
            // A reset that fails because the sweep dropped the database underneath it is expected at the end of the
            // process. What the sweep could not drop is dropped here, like any database that is returned after disposal.
            if (_disposed)
            {
                Drop(name);
                return;
            }

            // A database in an unknown state is never leased again.
            DropAfterFailure(name, resetFailure);
            throw;
        }

        if (schemaChanged)
        {
            Drop(name);
            return;
        }

        idle.Push(name);
    }

    /// <summary>
    /// Describes the structure of a database, as its server does, and its migration history, which a reset keeps.
    /// </summary>
    /// <param name="name">The name of the database.</param>
    /// <returns>A value that is equal for databases of the same structure and history.</returns>
    private string Fingerprint(string name)
    {
        using var context = new JellyfinDbContext(
            _server.CreateOptions(name, []),
            NullLogger<JellyfinDbContext>.Instance,
            _server.CreateProvider(),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
        var history = context.GetService<IHistoryRepository>();
        var applied = history.Exists()
            ? history.GetAppliedMigrations().Select(row => row.MigrationId + '\u001f' + row.ProductVersion)
            : [];
        return _server.GetSchemaFingerprint(name) + '\u001d' + string.Join('\u001e', applied);
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
