using System.IO;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    private const string SqliteProject = "Jellyfin.Database.Providers.Sqlite";

    /// <inheritdoc />
    public void ConfigureDesignTimeServices(IServiceCollection services)
    {
        var assemblyDirectory = Path.GetDirectoryName(typeof(PostgresDesignTimeServices).Assembly.Location);
        var sqliteMigrations = assemblyDirectory is null ? null : FindSqliteMigrations(assemblyDirectory);
        // The design-time tools show the warnings of this logger, through their own reporter.
        // The logger factory of the tools is a scoped service, so the generator is scoped as well.
        services.AddScoped<IMigrationsIdGenerator>(
            serviceProvider => new PairedMigrationsIdGenerator(
                sqliteMigrations,
                serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<PairedMigrationsIdGenerator>()));
    }

    /// <summary>
    /// Finds the migrations directory of the SQLite provider project from a directory below the repository,
    /// such as the output directory of this project or of the startup project of the tool.
    /// </summary>
    /// <param name="startDirectory">The directory to start the search in.</param>
    /// <returns>The migrations directory, or <c>null</c> when there is none.</returns>
    internal static string? FindSqliteMigrations(string startDirectory)
    {
        for (var directory = startDirectory; directory is not null; directory = Path.GetDirectoryName(directory))
        {
            foreach (var candidate in new[]
            {
                Path.Combine(directory, SqliteProject, "Migrations"),
                Path.Combine(directory, "src", "Jellyfin.Database", SqliteProject, "Migrations")
            })
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
