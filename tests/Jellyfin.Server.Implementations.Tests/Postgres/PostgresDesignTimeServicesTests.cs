using System;
using System.IO;
using System.Linq;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Providers.Postgres.Migrations;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public sealed class PostgresDesignTimeServicesTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("design-time-services").FullName;

    [Fact]
    public void ConfigureDesignTimeServices_RegistersThePairedIdGenerator()
    {
        // EF Core registers its logger factory as a scoped service, so the services are validated the way the tools run them.
        var services = new ServiceCollection();
        services.AddScoped<ILoggerFactory>(_ => NullLoggerFactory.Instance);

        new PostgresDesignTimeServices().ConfigureDesignTimeServices(services);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        Assert.IsType<PairedMigrationsIdGenerator>(scope.ServiceProvider.GetRequiredService<IMigrationsIdGenerator>());
    }

    [Fact]
    public void Assembly_ReferencesTheDesignTimeServices_ForAnotherStartupProject()
    {
        var reference = Assert.Single(typeof(PostgresDatabaseProvider).Assembly.GetCustomAttributes(typeof(DesignTimeServicesReferenceAttribute), false)
            .Cast<DesignTimeServicesReferenceAttribute>());

        Assert.Same(typeof(PostgresDesignTimeServices), Type.GetType(reference.TypeName, throwOnError: true));
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", reference.ForProvider);
    }

    [Fact]
    public void FindSqliteMigrations_FromTheOutputDirectoryOfTheProject_FindsTheDirectory()
    {
        var expected = CreateSqliteMigrations();
        var output = Directory.CreateDirectory(Path.Combine(_root, "src", "Jellyfin.Database", "Jellyfin.Database.Providers.Postgres", "bin", "Debug", "net10.0"));

        Assert.Equal(expected, PostgresDesignTimeServices.FindSqliteMigrations(output.FullName));
    }

    [Fact]
    public void FindSqliteMigrations_FromTheOutputDirectoryOfAnotherProject_FindsTheDirectory()
    {
        var expected = CreateSqliteMigrations();
        var output = Directory.CreateDirectory(Path.Combine(_root, "Jellyfin.Server", "bin", "Debug", "net10.0"));

        Assert.Equal(expected, PostgresDesignTimeServices.FindSqliteMigrations(output.FullName));
    }

    [Fact]
    public void FindSqliteMigrations_WithoutTheSqliteProject_ReturnsNull()
    {
        var output = Directory.CreateDirectory(Path.Combine(_root, "elsewhere", "bin"));

        Assert.Null(PostgresDesignTimeServices.FindSqliteMigrations(output.FullName));
    }

    [Fact]
    public void FindSqliteMigrations_FromTheTestOutput_FindsTheMigrationsOfTheRepository()
    {
        var directory = PostgresDesignTimeServices.FindSqliteMigrations(AppContext.BaseDirectory);

        Assert.NotNull(directory);
        Assert.True(File.Exists(Path.Combine(directory, "JellyfinDbModelSnapshot.cs")), directory);
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
    }

    private string CreateSqliteMigrations()
    {
        return Directory.CreateDirectory(Path.Combine(_root, "src", "Jellyfin.Database", "Jellyfin.Database.Providers.Sqlite", "Migrations")).FullName;
    }
}
