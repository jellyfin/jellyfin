using System;
using System.Linq;
using Jellyfin.Database.Implementations.DesignTime;
using Jellyfin.Database.Providers.Postgres;
using Jellyfin.Database.Providers.Postgres.Migrations;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public sealed class PostgresDesignTimeServicesTests
{
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
}
