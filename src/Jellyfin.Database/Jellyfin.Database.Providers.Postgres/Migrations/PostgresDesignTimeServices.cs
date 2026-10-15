using Jellyfin.Database.Implementations.DesignTime;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

// The attribute makes the services visible when the startup project of the tool is another project,
// because the tool only looks for IDesignTimeServices in the startup assembly.
[assembly: DesignTimeServicesReference(
    "Jellyfin.Database.Providers.Postgres.Migrations.PostgresDesignTimeServices, Jellyfin.Database.Providers.Postgres",
    "Npgsql.EntityFrameworkCore.PostgreSQL")]

namespace Jellyfin.Database.Providers.Postgres.Migrations;

/// <summary>
/// The design time services of the PostgreSQL provider.
/// They are only used for the creation of migrations and not during runtime.
/// </summary>
internal sealed class PostgresDesignTimeServices : IDesignTimeServices
{
    /// <inheritdoc />
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        PairedMigrationsDesignTimeServices.AddPairedMigrationsIdGenerator(services, typeof(PostgresDesignTimeServices).Assembly);
    }
}
