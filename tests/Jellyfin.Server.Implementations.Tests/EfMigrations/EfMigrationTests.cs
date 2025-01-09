using System;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Migrations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

public class EfMigrationTests
{
    [Fact]
    public void CheckForUnappliedMigrations()
    {
        var dbDesignContext = new DesignTimeJellyfinDbFactory();
        var context = dbDesignContext.CreateDbContext([]);
        Assert.False(context.Database.HasPendingModelChanges(), "There are unapplied changes to the EfCore model. Please create a Migration.");
    }
}
