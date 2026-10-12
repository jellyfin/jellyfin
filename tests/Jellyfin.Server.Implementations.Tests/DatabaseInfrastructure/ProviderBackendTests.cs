using System;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public class ProviderBackendTests
{
    [Fact]
    public void ProviderBackend_IsLive()
    {
        var backend = TestDatabaseEnvironment.Backend;
        var floor = ServerVersionFloors.For(backend.Provider);

        Assert.True(backend.ServerVersion.Major > 0);
        Assert.True(backend.ServerVersion >= floor, $"{backend.Provider} {backend.ServerVersion} is older than the supported {floor}.");
    }

    [Theory]
    [InlineData(TestSchemaSource.Model)]
    [InlineData(TestSchemaSource.Migrations)]
    public void Create_EverySchemaSource_ProducesUsableDatabase(TestSchemaSource source)
    {
        using var database = TestDatabaseEnvironment.Backend.Create(source, []);
        using var context = new JellyfinDbContext(
            database.Options,
            NullLogger<JellyfinDbContext>.Instance,
            database.CreateProvider(),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));

        var id = Guid.NewGuid();
        context.BaseItems.Add(new BaseItemEntity { Id = id, Type = "Test", IsFolder = false });
        context.SaveChanges();

        Assert.True(context.BaseItems.Any(e => e.Id.Equals(id)));
    }
}
