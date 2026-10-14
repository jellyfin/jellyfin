using System;
using System.Linq;
using System.Reflection;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Providers.MySql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

public class MySqlDesignTimeFactoryTests
{
    private const string FactoryTypeName = "Jellyfin.Database.Providers.MySql.Migrations.MySqlDesignTimeJellyfinDbFactory";

    [Fact]
    public void CreateDbContext_BuildsTheMySqlModelWithoutAServer()
    {
        using var context = CreateContext();

        Assert.Equal("Microting.EntityFrameworkCore.MySql", context.Database.ProviderName);
        Assert.NotNull(context.Model.FindEntityType(typeof(BaseItemEntity)));
    }

    [Fact]
    public void CreateDbContext_UsesTheMigrationsOfTheMySqlAssembly()
    {
        using var context = CreateContext();

        Assert.Same(typeof(MySqlDatabaseProvider).Assembly, context.GetService<IMigrationsAssembly>().Assembly);
    }

    [Fact]
    public void CreateDbContext_ModelHasNoPendingChangesWithoutAServer()
    {
        using var context = CreateContext();

        Assert.False(context.Database.HasPendingModelChanges());
    }

    private static JellyfinDbContext CreateContext()
    {
        var factoryType = typeof(MySqlDatabaseProvider).Assembly.GetType(FactoryTypeName, throwOnError: true)!;
        var factory = Activator.CreateInstance(factoryType, nonPublic: true)!;
        var create = factoryType.GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(e => e.Name == "CreateDbContext");

        return (JellyfinDbContext)create.Invoke(factory, [Array.Empty<string>()])!;
    }
}
