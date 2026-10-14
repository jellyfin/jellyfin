using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

/// <summary>
/// The migrations id generator and the design time services of the MySQL and PostgreSQL providers are copies of each other,
/// because a file linked into both provider assemblies would exist twice in the test projects that see their internals.
/// The tests keep the copies from drifting apart by comparing their source.
/// </summary>
public static partial class ProviderSourceParityTests
{
    private const string MySqlMigrations = "Jellyfin.Database.Providers.MySql/Migrations";
    private const string PostgresMigrations = "Jellyfin.Database.Providers.Postgres/Migrations";

    [GeneratedRegex(@"\[assembly:[^\]]*\]")]
    private static partial Regex AssemblyAttribute();

    [GeneratedRegex(@"Jellyfin\.Database\.Providers\.(MySql|Postgres)")]
    private static partial Regex ProviderNamespace();

    [GeneratedRegex(@"\b(MySql|Postgres)DesignTimeServices\b")]
    private static partial Regex DesignTimeServicesType();

    [Fact]
    public static void PairedMigrationsIdGenerator_CopiesOfTheProviders_HaveTheSameCode()
    {
        AssertSameCode(
            Path.Combine(MySqlMigrations, "PairedMigrationsIdGenerator.cs"),
            Path.Combine(PostgresMigrations, "PairedMigrationsIdGenerator.cs"));
    }

    [Fact]
    public static void DesignTimeServices_CopiesOfTheProviders_HaveTheSameCode()
    {
        AssertSameCode(
            Path.Combine(MySqlMigrations, "MySqlDesignTimeServices.cs"),
            Path.Combine(PostgresMigrations, "PostgresDesignTimeServices.cs"));
    }

    [Fact]
    public static void Normalise_DifferencesOfNamesAndCommentsOnly_AreIgnored()
    {
        var mySql = Normalise(
            """
            // A comment
            [assembly: Reference("a", "b")]
            namespace Jellyfin.Database.Providers.MySql.Migrations;

            /// <summary>Docs.</summary>
            internal sealed class MySqlDesignTimeServices { }
            """);
        var postgres = Normalise(
            """
            [assembly: Reference("c", "d")]
            namespace Jellyfin.Database.Providers.Postgres.Migrations;
            internal sealed class PostgresDesignTimeServices { }
            """);

        Assert.Equal(postgres, mySql);
    }

    [Fact]
    public static void Normalise_DifferenceInCode_IsKept()
    {
        var original = Normalise("internal sealed class MySqlDesignTimeServices { int a = 1; }");
        var changed = Normalise("internal sealed class PostgresDesignTimeServices { int a = 2; }");

        Assert.NotEqual(original, changed);
    }

    /// <summary>
    /// Reduces the source to its code with the names of the provider replaced, so that what is compared is what the copies do.
    /// </summary>
    private static List<string> Normalise(string source)
    {
        source = AssemblyAttribute().Replace(source, string.Empty);
        source = ProviderNamespace().Replace(source, "Jellyfin.Database.Providers.Provider");
        source = DesignTimeServicesType().Replace(source, "DesignTimeServices");

        return source
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal))
            .ToList();
    }

    private static void AssertSameCode(string mySqlFile, string postgresFile)
    {
        var providers = FindProvidersDirectory();
        var mySql = Normalise(File.ReadAllText(Path.Combine(providers, mySqlFile)));
        var postgres = Normalise(File.ReadAllText(Path.Combine(providers, postgresFile)));

        Assert.NotEmpty(mySql);
        for (var i = 0; i < Math.Max(mySql.Count, postgres.Count); i++)
        {
            var left = i < mySql.Count ? mySql[i] : "(end of file)";
            var right = i < postgres.Count ? postgres[i] : "(end of file)";
            Assert.True(
                left == right,
                $"{mySqlFile} and {postgresFile} differ at code line {i + 1}: '{left}' against '{right}'. Change both copies alike.");
        }
    }

    private static string FindProvidersDirectory()
    {
        for (var directory = AppContext.BaseDirectory; directory is not null; directory = Path.GetDirectoryName(directory))
        {
            var candidate = Path.Combine(directory, "src", "Jellyfin.Database");
            if (Directory.Exists(Path.Combine(candidate, "Jellyfin.Database.Providers.MySql")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException($"The source of the providers was not found above {AppContext.BaseDirectory}.");
    }
}
