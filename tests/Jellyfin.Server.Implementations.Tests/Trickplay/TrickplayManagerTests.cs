using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Server.Implementations.Tests.Item;
using Jellyfin.Server.Implementations.Trickplay;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Trickplay;

public sealed class TrickplayManagerTests : SqliteDbTestFixture
{
    [Fact]
    public async Task DeleteTrickplayDataAsync_DisposesDbContext()
    {
        var contexts = new List<JellyfinDbContext>();
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var context = CreateDbContext();
                contexts.Add(context);
                return context;
            });

        // Only the database context factory is used when deleting trickplay data.
        var trickplayManager = new TrickplayManager(
            NullLogger<TrickplayManager>.Instance,
            null!,
            null!,
            null!,
            null!,
            null!,
            factory.Object,
            null!,
            null!);

        await trickplayManager.DeleteTrickplayDataAsync(Guid.NewGuid(), CancellationToken.None);

        var context = Assert.Single(contexts);
        Assert.Throws<ObjectDisposedException>(() => context.Model);
    }
}
