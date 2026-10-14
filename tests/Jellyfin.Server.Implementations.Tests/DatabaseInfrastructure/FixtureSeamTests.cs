using System;
using System.IO;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// Tests how <see cref="DatabaseTestFixture"/> asks its backend for a database, which is the one place where a test
/// says that it needs a database of its own.
/// </summary>
public sealed class FixtureSeamTests : IDisposable
{
#pragma warning disable RS0030 // The seam is tested on SQLite files whichever provider is selected
    private readonly SqliteFileTestBackend _backend = new();
#pragma warning restore RS0030

    public void Dispose()
    {
        _backend.Dispose();
    }

    [Fact]
    public void Fixture_NotFresh_ReturnsItsDatabaseToThePool()
    {
        string name;
        using (var fixture = new ProbeFixture(_backend, fresh: false))
        {
            name = fixture.ConnectionString;
            Assert.Single(Directory.GetFiles(_backend.DatabaseDirectory));
        }

        Assert.Single(Directory.GetFiles(_backend.DatabaseDirectory));
        using var next = new ProbeFixture(_backend, fresh: false);
        Assert.Equal(name, next.ConnectionString);
    }

    [Fact]
    public void Fixture_Fresh_UsesADatabaseOutsideThePoolThatIsDroppedWithTheFixture()
    {
        string pooled;
        using (var fixture = new ProbeFixture(_backend, fresh: false))
        {
            pooled = fixture.ConnectionString;
        }

        using (var fresh = new ProbeFixture(_backend, fresh: true))
        {
            Assert.NotEqual(pooled, fresh.ConnectionString);
            Assert.Equal(2, Directory.GetFiles(_backend.DatabaseDirectory).Length);
        }

        Assert.Single(Directory.GetFiles(_backend.DatabaseDirectory));
        using var next = new ProbeFixture(_backend, fresh: false);
        Assert.Equal(pooled, next.ConnectionString);
    }

    [ProviderFact(TestDbProvider.MySql, TestDbProvider.MariaDb)]
    public void Fixture_FreshOnTheSelectedBackend_UsesADatabaseOutsideThePoolThatIsDroppedWithTheFixture()
    {
        var backend = TestDatabaseEnvironment.Backend;
        string pooled;
        using (var fixture = new ProbeFixture(backend, fresh: false))
        {
            pooled = fixture.ConnectionString;
        }

        ProbeFixture fresh;
        using (fresh = new ProbeFixture(backend, fresh: true))
        {
            Assert.NotEqual(pooled, fresh.ConnectionString);
            Assert.True(fresh.CanConnect());
        }

        Assert.False(fresh.CanConnect());
        using var next = new ProbeFixture(backend, fresh: false);
        Assert.Equal(pooled, next.ConnectionString);
    }

    private sealed class ProbeFixture(ITestDatabaseBackend backend, bool fresh)
        : DatabaseTestFixture(backend, TestSchemaSource.Model, fresh)
    {
        public string ConnectionString
        {
            get
            {
                using var context = CreateDbContext();
                return context.Database.GetConnectionString()!;
            }
        }

        public bool CanConnect()
        {
            using var context = CreateDbContext();
            return context.Database.CanConnect();
        }
    }
}
