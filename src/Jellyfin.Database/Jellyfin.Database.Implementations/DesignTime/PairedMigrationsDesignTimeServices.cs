using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Implementations.DesignTime;

/// <summary>
/// The design time services that every provider which mirrors the migrations of the SQLite provider shares.
/// They are only used for the creation of migrations and not during runtime.
/// </summary>
/// <remarks>
/// They live here, and not in a provider, because a provider may only reference this assembly (ADR-0011).
/// Each provider still has its own <c>IDesignTimeServices</c> and <c>DesignTimeServicesReference</c>,
/// because the attribute names the provider it applies to, and calls <see cref="AddPairedMigrationsIdGenerator"/> from there.
/// </remarks>
internal static class PairedMigrationsDesignTimeServices
{
    private const string SqliteProject = "Jellyfin.Database.Providers.Sqlite";

    /// <summary>
    /// Registers the <see cref="PairedMigrationsIdGenerator"/>, which reuses the ids of the SQLite migrations,
    /// with the migrations directory of the SQLite provider found from the output directory of the provider.
    /// </summary>
    /// <param name="services">The design time services of the provider.</param>
    /// <param name="providerAssembly">The assembly of the provider that holds the migrations.</param>
    public static void AddPairedMigrationsIdGenerator(IServiceCollection services, Assembly providerAssembly)
    {
        var assemblyDirectory = Path.GetDirectoryName(providerAssembly.Location);
        var sqliteMigrations = string.IsNullOrEmpty(assemblyDirectory) ? null : FindSqliteMigrations(assemblyDirectory);
        // The design-time tools show the warnings of this logger, through their own reporter.
        // The logger factory of the tools is a scoped service, so the generator is scoped as well.
        services.AddScoped<IMigrationsIdGenerator>(
            serviceProvider => new PairedMigrationsIdGenerator(
                sqliteMigrations,
                serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<PairedMigrationsIdGenerator>()));
    }

    /// <summary>
    /// Finds the migrations directory of the SQLite provider project from a directory below the repository,
    /// such as the output directory of a provider project or of the startup project of the tool.
    /// </summary>
    /// <param name="startDirectory">The directory to start the search in.</param>
    /// <returns>The migrations directory, or <c>null</c> when there is none.</returns>
    public static string? FindSqliteMigrations(string startDirectory)
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
