using System;
using System.Linq;
using System.Reflection;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Providers.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SqlServer;

public class SqlServerDesignTimeFactoryTests
{
    private const string FactoryTypeName = "Jellyfin.Database.Providers.SqlServer.Migrations.SqlServerDesignTimeJellyfinDbFactory";

    [Fact]
    public void CreateDbContext_BuildsTheSqlServerModelWithoutAServer()
    {
        using var context = CreateContext();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.NotNull(context.Model.FindEntityType(typeof(BaseItemEntity)));
    }

    [Fact]
    public void CreateDbContext_UsesTheMigrationsOfTheSqlServerAssembly()
    {
        using var context = CreateContext();

        Assert.Same(typeof(SqlServerDatabaseProvider).Assembly, context.GetService<IMigrationsAssembly>().Assembly);
    }

    [Fact]
    public void CreateDbContext_ModelHasNoPendingChangesWithoutAServer()
    {
        using var context = CreateContext();

        Assert.False(context.Database.HasPendingModelChanges());
    }

    private static JellyfinDbContext CreateContext()
    {
        var factoryType = typeof(SqlServerDatabaseProvider).Assembly.GetType(FactoryTypeName, throwOnError: true)!;
        var factory = Activator.CreateInstance(factoryType, nonPublic: true)!;
        var create = factoryType.GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(e => e.Name == "CreateDbContext");

        return (JellyfinDbContext)create.Invoke(factory, [Array.Empty<string>()])!;
    }
}
