using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests;

public class ApplyDatabaseSettingsTests
{
    [Fact]
    public async Task ApplyDatabaseSettingsAsync_ProviderRegistered_PassesItAContextFromTheFactoryAndDisposesIt()
    {
        var databaseProvider = new Mock<IJellyfinDatabaseProvider>();
        var options = new DbContextOptionsBuilder<JellyfinDbContext>().UseSqlite("Data Source=:memory:").Options;
        var context = new JellyfinDbContext(options, NullLogger<JellyfinDbContext>.Instance, databaseProvider.Object, new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
        var contextFactory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        contextFactory
            .Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);
        JellyfinDbContext? received = null;
        var disposedWhenCalled = true;
        databaseProvider
            .Setup(p => p.ApplyDatabaseSettingsAsync(It.IsAny<JellyfinDbContext>(), It.IsAny<CancellationToken>()))
            .Callback((JellyfinDbContext dbContext, CancellationToken _) =>
            {
                received = dbContext;
                disposedWhenCalled = Record.Exception(() => dbContext.Database) is ObjectDisposedException;
            })
            .Returns(Task.CompletedTask);
        using var tokenSource = new CancellationTokenSource();
        await using var services = new ServiceCollection()
            .AddSingleton(databaseProvider.Object)
            .AddSingleton(contextFactory.Object)
            .BuildServiceProvider();

        await Program.ApplyDatabaseSettingsAsync(services, tokenSource.Token);

        Assert.Same(context, received);
        Assert.False(disposedWhenCalled);
        Assert.Throws<ObjectDisposedException>(() => context.Database);
        databaseProvider.Verify(p => p.ApplyDatabaseSettingsAsync(context, tokenSource.Token), Times.Once);
    }
}
