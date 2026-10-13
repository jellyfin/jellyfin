using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// Tests the pool when it is disposed while databases are idle or leased, on a server that can be made to fail.
/// </summary>
public sealed class LeasedPoolTeardownTests : IDisposable
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "The pool disposes the server.")]
    private readonly FaultInjectingServer _server = new();
    private readonly LeasedDatabasePool _pool;

    public LeasedPoolTeardownTests()
    {
        _pool = new LeasedDatabasePool(_server);
    }

    public void Dispose()
    {
        _pool.Dispose();
    }

    [Fact]
    public void Lease_Fresh_DoesNotComputeAFingerprint()
    {
        using var database = _pool.Lease(TestSchemaSource.Model, [], fresh: true);

        Assert.Equal(0, _server.FingerprintCalls);
    }

    [Fact]
    public void Lease_Pooled_ComputesTheFingerprintOnceWhenTheDatabaseIsBuiltAndOnceWhenItIsReturned()
    {
        var database = _pool.Lease(TestSchemaSource.Model, [], fresh: false);
        Assert.Equal(1, _server.FingerprintCalls);

        database.Dispose();

        Assert.Equal(2, _server.FingerprintCalls);
    }

    [Fact]
    public void Lease_WhenThePoolIsDisposedAfterTheFirstCheck_DoesNotHandOutTheSweptIdleDatabase()
    {
        _pool.Lease(TestSchemaSource.Model, [], fresh: false).Dispose();

        // The lease has passed its first check of disposal and is about to take the idle database when the pool is
        // disposed, so only the check inside the lock can refuse it.
        _pool.BeforeIdleDatabaseIsTaken = _pool.Dispose;

        Assert.Throws<ObjectDisposedException>(() => _pool.Lease(TestSchemaSource.Model, [], fresh: false));

        Assert.Single(_server.Dropped);
    }

    [Fact]
    public void Dispose_LeaseWhoseResetFailsWhileThePoolIsDisposed_DropsItLikeAnyDatabaseReturnedAfterDisposal()
    {
        var database = _pool.Lease(TestSchemaSource.Model, [], fresh: false);
        _server.OnFingerprint = calls =>
        {
            if (calls == 2)
            {
                // The sweep fails to drop the database, and then the check of the schema fails.
                _server.FailDrops = true;
                _pool.Dispose();
                throw new InvalidOperationException("The reset failed.");
            }
        };

        var failure = Assert.ThrowsAny<Exception>(database.Dispose);

        Assert.IsNotType<AggregateException>(failure);
        Assert.IsType<IOException>(failure);
    }

    private sealed class FaultInjectingServer : IPooledDatabaseServer
    {
        private readonly SqliteFileServer _inner = new();
        private int _fingerprintCalls;

        public int FingerprintCalls => _fingerprintCalls;

        public Action<int>? OnFingerprint { get; set; }

        public bool FailDrops { get; set; }

        public ConcurrentQueue<string> Dropped { get; } = new();

        public string CreateDatabase() => _inner.CreateDatabase();

        public DbContextOptions<JellyfinDbContext> CreateOptions(string database, IInterceptor[] interceptors) => _inner.CreateOptions(database, interceptors);

        public IJellyfinDatabaseProvider CreateProvider() => _inner.CreateProvider();

        public string GetSchemaFingerprint(string database)
        {
            var calls = Interlocked.Increment(ref _fingerprintCalls);
            OnFingerprint?.Invoke(calls);
            return _inner.GetSchemaFingerprint(database);
        }

        public void DropDatabase(string database)
        {
            if (FailDrops)
            {
                throw new IOException("The drop failed.");
            }

            _inner.DropDatabase(database);
            Dropped.Enqueue(database);
        }

        public void Dispose()
        {
            _inner.Dispose();
        }
    }
}
