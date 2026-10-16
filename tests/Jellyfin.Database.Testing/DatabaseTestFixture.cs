using System;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using MediaBrowser.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jellyfin.Database.Testing;

/// <summary>
/// Base fixture for tests that need a database: one empty database per test instance, on the provider selected by
/// <see cref="TestDatabaseEnvironment"/>. Its schema comes from the migrations unless the derived class asks for the
/// model, or the provider of the engine has no migrations (<see cref="EngineTraits.DefaultSchemaSource"/>). Derived
/// classes seed in their own constructor.
/// </summary>
public abstract class DatabaseTestFixture : IDisposable
{
    private readonly TestDatabase _database;
    private readonly ITestDatabaseBackend _backend;

    protected DatabaseTestFixture(params IInterceptor[] interceptors)
        : this(EngineTraits.Selected.DefaultSchemaSource, interceptors)
    {
    }

    protected DatabaseTestFixture(TestSchemaSource schemaSource, params IInterceptor[] interceptors)
        : this(schemaSource, false, interceptors)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseTestFixture"/> class.
    /// </summary>
    /// <param name="schemaSource">Where the schema of the database comes from.</param>
    /// <param name="fresh">Whether the test changes the schema and so needs a database that no other test has used or will use.</param>
    /// <param name="interceptors">The interceptors that see the commands of the test.</param>
    protected DatabaseTestFixture(TestSchemaSource schemaSource, bool fresh, params IInterceptor[] interceptors)
        : this(TestDatabaseEnvironment.Backend, schemaSource, fresh, interceptors)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseTestFixture"/> class on a given backend.
    /// </summary>
    /// <param name="backend">The backend that creates the database.</param>
    /// <param name="schemaSource">Where the schema of the database comes from.</param>
    /// <param name="fresh">Whether the test changes the schema and so needs a database that no other test has used or will use.</param>
    /// <param name="interceptors">The interceptors that see the commands of the test.</param>
    protected DatabaseTestFixture(ITestDatabaseBackend backend, TestSchemaSource schemaSource, bool fresh, params IInterceptor[] interceptors)
    {
        ApplicationPaths = new Mock<IApplicationPaths>().Object;

        _backend = backend;
        _database = backend.Create(schemaSource, interceptors, fresh);
        try
        {
            DatabaseProvider = _database.CreateProvider();
            DatabaseProvider.DbContextFactory = CreateDbContextFactory();
        }
        catch
        {
            _database.Dispose();
            throw;
        }
    }

    protected IApplicationPaths ApplicationPaths { get; }

    protected TestDbProvider DbProvider => _backend.Provider;

    protected IJellyfinDatabaseProvider DatabaseProvider { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected JellyfinDbContext CreateDbContext() => new(
        _database.Options,
        NullLogger<JellyfinDbContext>.Instance,
        DatabaseProvider,
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    protected IDbContextFactory<JellyfinDbContext> CreateDbContextFactory() => new TestDbContextFactory(CreateDbContext);

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _database.Dispose();
        }
    }
}
