using System;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Testing.Conformance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Conformance;

/// <summary>
/// Holds the comparison of the model row to what it may allow an engine without partial indexes: a filtered index that
/// is not unique may be over every row, and nothing else may differ. The models are built by SQLite and by the MySQL
/// provider, whose conventions remove every filter, without a server.
/// </summary>
public class ModelShapeTests
{
    [Fact]
    public void ShapeDifferences_NonUniqueIndexThatLostItsFilter_IsAllowedWithoutPartialIndexes()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false);
        using var actual = new ProbeContext(mySql: true, uniqueFiltered: false);

        Assert.Empty(ModelConformance.ShapeDifferences(reference.Model, actual.Model, supportsPartialIndexes: false));
    }

    [Fact]
    public void ShapeDifferences_NonUniqueIndexThatLostItsFilter_FailsWithPartialIndexes()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false);
        using var actual = new ProbeContext(mySql: true, uniqueFiltered: false);

        var problems = ModelConformance.ShapeDifferences(reference.Model, actual.Model, supportsPartialIndexes: true);

        Assert.Contains("missing: Probes index (Rank), filtered", problems);
        Assert.Contains("not in the reference: Probes index (Rank)", problems);
        Assert.Equal(2, problems.Count);
    }

    [Fact]
    public void ShapeDifferences_UniqueIndexThatLostItsFilter_FailsWithoutPartialIndexes()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: true);
        using var actual = new ProbeContext(mySql: true, uniqueFiltered: true);

        var problems = ModelConformance.ShapeDifferences(reference.Model, actual.Model, supportsPartialIndexes: false);

        Assert.Contains("missing: Probes unique index (Name), filtered", problems);
        Assert.Contains("not in the reference: Probes unique index (Name)", problems);
        Assert.Equal(2, problems.Count);
    }

    [Fact]
    public void ShapeDifferences_IndexThatKeptItsFilter_MatchesWithoutPartialIndexes()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: true);

        Assert.Empty(ModelConformance.ShapeDifferences(reference.Model, reference.Model, supportsPartialIndexes: false));
    }

    [Fact]
    public void ShapeDifferences_ReferenceWithoutAFilteredIndexThatIsNotUnique_FailsWithoutPartialIndexes()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false);

        var problems = ModelConformance.ShapeDifferences(reference.Model, reference.Model, supportsPartialIndexes: false);

        Assert.Contains(problems, problem => problem.Contains("nothing for the allowance to allow", StringComparison.Ordinal));
        Assert.Empty(ModelConformance.ShapeDifferences(reference.Model, reference.Model, supportsPartialIndexes: true));
    }

    [Fact]
    public void ShapeDifferences_OtherDifference_FailsWithoutPartialIndexes()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false);
        using var actual = new ProbeContext(mySql: true, uniqueFiltered: false, rankIsUnique: true);

        var problems = ModelConformance.ShapeDifferences(reference.Model, actual.Model, supportsPartialIndexes: false);

        Assert.Contains("missing: Probes index (Rank), filtered", problems);
        Assert.Contains("not in the reference: Probes unique index (Rank)", problems);
    }

    private sealed class Probe
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public int Rank { get; set; }
    }

    // A model with a unique and a non-unique index on one table, each filtered or not, built by SQLite or by the
    // provider of MySQL and MariaDB as it builds the model of Jellyfin.
    private sealed class ProbeContext(bool mySql, bool uniqueFiltered, bool nonUniqueFiltered = true, bool rankIsUnique = false) : DbContext
    {
        private readonly MySqlDatabaseProvider _provider = new(NullLogger<MySqlDatabaseProvider>.Instance);

        public (bool MySql, bool UniqueFiltered, bool NonUniqueFiltered, bool RankIsUnique) Key => (mySql, uniqueFiltered, nonUniqueFiltered, rankIsUnique);

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (mySql)
            {
                _provider.Initialise(
                    optionsBuilder,
                    new DatabaseConfigurationOptions
                    {
                        DatabaseType = "Jellyfin-MySQL",
                        CustomProviderOptions = new CustomDatabaseOptions
                        {
                            PluginName = string.Empty,
                            PluginAssembly = string.Empty,
                            ConnectionString = "Server=localhost;Database=jellyfin_offline",
                            Options = [new CustomDatabaseOption { Key = "server-version", Value = "8.4-mysql" }]
                        }
                    });
            }
            else
            {
#pragma warning disable RS0030 // Do not use banned APIs: The context is only inspected offline and never opens a database.
                optionsBuilder.UseSqlite("Data Source=:memory:");
#pragma warning restore RS0030
            }

            // Every variant builds a model of its own, rather than the one EF Core cached for the type.
            optionsBuilder.ReplaceService<IModelCacheKeyFactory, ProbeModelCacheKeyFactory>();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            if (mySql)
            {
                _provider.ConfigureConventions(configurationBuilder);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (mySql)
            {
                _provider.OnModelCreating(modelBuilder);
            }

            modelBuilder.Entity<Probe>(b =>
            {
                b.ToTable("Probes");
                b.Property(e => e.Name).HasMaxLength(64);
                var unique = b.HasIndex(e => e.Name).IsUnique();
                if (uniqueFiltered)
                {
                    unique.HasFilter("\"Name\" IS NOT NULL");
                }

                var rank = b.HasIndex(e => e.Rank).IsUnique(rankIsUnique);
                if (nonUniqueFiltered)
                {
                    rank.HasFilter("\"Rank\" > 0");
                }
            });
        }
    }

    private sealed class ProbeModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) => (context.GetType(), ((ProbeContext)context).Key, designTime);
    }
}
