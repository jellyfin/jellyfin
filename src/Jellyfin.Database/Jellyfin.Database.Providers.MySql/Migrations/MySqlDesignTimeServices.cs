using Jellyfin.Database.Implementations.DesignTime;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

// The attribute makes the services visible when the startup project of the tool is another project,
// because the tool only looks for IDesignTimeServices in the startup assembly.
[assembly: DesignTimeServicesReference(
    "Jellyfin.Database.Providers.MySql.Migrations.MySqlDesignTimeServices, Jellyfin.Database.Providers.MySql",
    "Microting.EntityFrameworkCore.MySql")]

namespace Jellyfin.Database.Providers.MySql.Migrations;

/// <summary>
/// The design time services of the MySQL and MariaDB provider.
/// They are only used for the creation of migrations and not during runtime.
/// </summary>
internal sealed class MySqlDesignTimeServices : IDesignTimeServices
{
    /// <inheritdoc />
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        PairedMigrationsDesignTimeServices.AddPairedMigrationsIdGenerator(services, typeof(MySqlDesignTimeServices).Assembly);
    }
}
