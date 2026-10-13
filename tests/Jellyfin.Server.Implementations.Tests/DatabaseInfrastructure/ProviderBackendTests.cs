using System;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public class ProviderBackendTests(ITestOutputHelper output)
{
    [Fact]
    public void ProviderBackend_IsLive()
    {
        var backend = TestDatabaseEnvironment.Backend;

        BackendLiveness.Verify(Environment.GetEnvironmentVariable(BackendLiveness.ExpectedProviderVariable), backend);

        output.WriteLine($"Database backend: {backend.Provider} {backend.ServerVersion}");
    }

    [Fact]
    public void Create_ModelSchema_ProducesUsableDatabase()
    {
        AssertUsable(TestSchemaSource.Model);
    }

    [Fact]
    public void Create_MigrationsSchema_ProducesUsableDatabase()
    {
        AssertUsable(TestSchemaSource.Migrations);
    }

    [Fact]
    public void Create_FreshModelSchema_ProducesUsableDatabase()
    {
        AssertUsable(TestSchemaSource.Model, fresh: true);
    }

    [Fact]
    public void Create_FreshMigrationsSchema_ProducesUsableDatabase()
    {
        AssertUsable(TestSchemaSource.Migrations, fresh: true);
    }

    [Fact]
    public void Create_FreshDatabases_DoNotShareData()
    {
        using var first = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);
        using var second = TestDatabaseEnvironment.Backend.Create(TestSchemaSource.Model, [], fresh: true);

        var id = Guid.NewGuid();
        using (var context = CreateContext(first))
        {
            context.BaseItems.Add(new BaseItemEntity { Id = id, Type = "Test", IsFolder = false });
            context.SaveChanges();
        }

        using var other = CreateContext(second);
        Assert.False(other.BaseItems.Any(e => e.Id.Equals(id)));
    }

    private static JellyfinDbContext CreateContext(TestDatabase database)
    {
        return new JellyfinDbContext(
            database.Options,
            NullLogger<JellyfinDbContext>.Instance,
            database.CreateProvider(),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }

    private static void AssertUsable(TestSchemaSource source, bool fresh = false)
    {
        using var database = TestDatabaseEnvironment.Backend.Create(source, [], fresh);
        using var context = CreateContext(database);

        var id = Guid.NewGuid();
        context.BaseItems.Add(new BaseItemEntity { Id = id, Type = "Test", IsFolder = false });
        context.SaveChanges();

        Assert.True(context.BaseItems.Any(e => e.Id.Equals(id)));
    }
}
