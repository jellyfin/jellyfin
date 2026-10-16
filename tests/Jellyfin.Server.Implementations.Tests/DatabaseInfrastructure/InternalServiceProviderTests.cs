using System;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

/// <summary>
/// Holds the options that the tests of the selected engine run on to sharing one internal service provider of EF Core,
/// whatever database and interceptors they are for. On a database server those are the options its provider builds, as
/// for the server; on SQLite they are the options of the test backend, which opens its databases in memory itself.
/// </summary>
/// <remarks>
/// EF Core keeps those service providers for the whole process, and throws only once it holds twenty, which a short run
/// never reaches. So this test, and not that limit, is what catches options that differ for every context. The tests
/// that build models of their own on purpose stay out of that cache.
/// </remarks>
public sealed class InternalServiceProviderTests
{
    [Fact]
    public void Contexts_OnTwoDatabasesOfTheSelectedEngine_ShareOneInternalServiceProvider()
    {
        var backend = TestDatabaseEnvironment.Backend;
        using var firstDatabase = backend.Create(TestSchemaSource.Model, []);
        using var secondDatabase = backend.Create(TestSchemaSource.Model, [new NoOpCommandInterceptor()]);
        using var first = CreateContext(firstDatabase);
        using var second = CreateContext(secondDatabase);

        Assert.False(AreTheSameDatabase(firstDatabase, secondDatabase));
        Assert.Same(first.GetService<IModelSource>(), second.GetService<IModelSource>());
    }

    // The options name the database by a connection string, or by a connection that an embedded engine keeps open, as
    // SQLite does for each of its databases in memory, which all have the same connection string.
    private static bool AreTheSameDatabase(TestDatabase first, TestDatabase second)
    {
        var a = RelationalOptionsExtension.Extract(first.Options);
        var b = RelationalOptionsExtension.Extract(second.Options);
        return a.Connection is not null || b.Connection is not null
            ? ReferenceEquals(a.Connection, b.Connection)
            : string.Equals(a.ConnectionString, b.ConnectionString, StringComparison.Ordinal);
    }

    private static JellyfinDbContext CreateContext(TestDatabase database)
        => new(
            database.Options,
            NullLogger<JellyfinDbContext>.Instance,
            database.CreateProvider(),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    private sealed class NoOpCommandInterceptor : DbCommandInterceptor;
}
