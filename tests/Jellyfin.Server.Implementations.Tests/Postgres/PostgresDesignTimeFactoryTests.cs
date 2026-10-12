using System;
using System.Linq;
using System.Reflection;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Providers.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public class PostgresDesignTimeFactoryTests
{
    private const string FactoryTypeName = "Jellyfin.Database.Providers.Postgres.Migrations.PostgresDesignTimeJellyfinDbFactory";

    [Fact]
    public void CreateDbContext_BuildsThePostgresModelWithoutAServer()
    {
        using var context = CreateContext();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
        Assert.NotNull(context.Model.FindEntityType(typeof(BaseItemEntity)));
    }

    [Fact]
    public void CreateDbContext_UsesTheMigrationsOfThePostgresAssembly()
    {
        using var context = CreateContext();

        Assert.Same(typeof(PostgresDatabaseProvider).Assembly, context.GetService<IMigrationsAssembly>().Assembly);
    }

    private static JellyfinDbContext CreateContext()
    {
        var factoryType = typeof(PostgresDatabaseProvider).Assembly.GetType(FactoryTypeName, throwOnError: true)!;
        var factory = Activator.CreateInstance(factoryType, nonPublic: true)!;
        var create = factoryType.GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(e => e.Name == "CreateDbContext");

        return (JellyfinDbContext)create.Invoke(factory, [Array.Empty<string>()])!;
    }
}
