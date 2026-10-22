using Jellyfin.Database.Implementations.DesignTime;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

// The attribute makes the services visible when the startup project of the tool is another project,
// because the tool only looks for IDesignTimeServices in the startup assembly.
[assembly: DesignTimeServicesReference(
    "Jellyfin.Database.Providers.SqlServer.Migrations.SqlServerDesignTimeServices, Jellyfin.Database.Providers.SqlServer",
    "Microsoft.EntityFrameworkCore.SqlServer")]

namespace Jellyfin.Database.Providers.SqlServer.Migrations;

/// <summary>
/// The design time services of the SQL Server provider.
/// They are only used for the creation of migrations and not during runtime.
/// </summary>
internal sealed class SqlServerDesignTimeServices : IDesignTimeServices
{
    /// <inheritdoc />
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        PairedMigrationsDesignTimeServices.AddPairedMigrationsIdGenerator(services, typeof(SqlServerDesignTimeServices).Assembly);
    }
}
