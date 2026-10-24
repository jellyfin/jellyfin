using System;
using System.Threading;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Base fixture for the tests that run against a real database. Each test class gets its own database, and the
/// fixture provides the contexts and context factory used to set up the repositories under test. Which database
/// engine backs it is up to the <see cref="TestDatabase"/> handed in. Derived classes seed in their own constructor.
/// </summary>
public abstract class DbTestFixture : IDisposable
{
    private readonly TestDatabase _database;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    protected DbTestFixture(TestDatabase database, params IInterceptor[] interceptors)
    {
        ApplicationPaths = new Mock<IApplicationPaths>().Object;

        _database = database;
        _dbOptions = database.Configure(new DbContextOptionsBuilder<JellyfinDbContext>())
            .AddInterceptors(interceptors)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    protected IApplicationPaths ApplicationPaths { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        _database.CreateProvider(ApplicationPaths),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

    protected IDbContextFactory<JellyfinDbContext> CreateDbContextFactory()
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDbContext);

        return factory.Object;
    }

    protected BaseItemRepository CreateBaseItemRepository(ItemTypeLookup itemTypeLookup)
    {
        var serverConfigurationManager = new Mock<IServerConfigurationManager>();
        serverConfigurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        return new BaseItemRepository(
            CreateDbContextFactory(),
            new Mock<IServerApplicationHost>().Object,
            itemTypeLookup,
            serverConfigurationManager.Object,
            NullLogger<BaseItemRepository>.Instance);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _database.Dispose();
        }
    }
}
