using System;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;

public sealed class FreshDatabaseFixtureTests : DatabaseTestFixture
{
    public FreshDatabaseFixtureTests()
        : base(TestSchemaSource.Model, fresh: true)
    {
    }

    [Fact]
    public void FreshDatabase_IsUsable()
    {
        using var context = CreateDbContext();

        context.ActivityLogs.Add(new ActivityLog("entry", "type", Guid.Empty));
        context.SaveChanges();

        Assert.Single(context.ActivityLogs.ToList());
    }
}
