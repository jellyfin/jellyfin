using System;
using System.Collections.Generic;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Testing;
using Jellyfin.Database.Testing.Conformance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Conformance;

/// <summary>
/// Holds the comparison of the model row to what it may allow an engine: a filtered index that is not unique may be
/// over every row, and a relationship that deletes in cascade may delete nothing, each only where the allowance names
/// it and where the engine does differ, and a unique index may be filtered to the rows where its columns have a value, by
/// exactly the filter the allowance names; nothing else may differ. The models are built by SQLite and by the MySQL
/// provider, whose conventions remove every filter, without a server.
/// </summary>
public class ModelShapeTests
{
    private const string RankFilter = "\"Rank\" > 0";
    private const string NameFilter = "\"Name\" IS NOT NULL";
    private const string NullNameFilter = "[Name] IS NOT NULL";
    private static readonly (string Table, string Columns) _parentKey = ("Probes", "ParentId");

    private static readonly IReadOnlyList<string> _noFilters = [];
    private static readonly IReadOnlyList<(string Table, string Columns)> _noCascades = [];
    private static readonly IReadOnlyList<(string Table, string Columns, string Filter)> _noUniques = [];
    private static readonly (string Table, string Columns, string Filter) _nameNotNull = ("Probes", "Name", NullNameFilter);

    [Fact]
    public void ShapeDifferences_NonUniqueIndexThatLostItsListedFilter_IsAllowed()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false);
        using var actual = new ProbeContext(mySql: true, uniqueFiltered: false);

        Assert.Empty(ModelConformance.ShapeDifferences(reference.Model, actual.Model, new ModelAllowances([RankFilter], _noCascades, _noUniques)));
    }

    [Fact]
    public void ShapeDifferences_NonUniqueIndexThatLostAFilterThatIsNotListed_Fails()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false);
        using var actual = new ProbeContext(mySql: true, uniqueFiltered: false);

        var problems = ModelConformance.ShapeDifferences(reference.Model, actual.Model, new ModelAllowances(_noFilters, _noCascades, _noUniques));

        Assert.Contains("missing: Probes index (Rank), filtered", problems);
        Assert.Contains("not in the reference: Probes index (Rank)", problems);
        Assert.Equal(2, problems.Count);
    }

    [Fact]
    public void ShapeDifferences_UniqueIndexThatLostItsListedFilter_Fails()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: true);
        using var actual = new ProbeContext(mySql: true, uniqueFiltered: true);

        var problems = ModelConformance.ShapeDifferences(reference.Model, actual.Model, new ModelAllowances([RankFilter, NameFilter], _noCascades, _noUniques));

        Assert.Contains(problems, problem => problem.StartsWith("allowance for a unique index: Probes unique index (Name), filtered", StringComparison.Ordinal));
        Assert.Contains("missing: Probes unique index (Name), filtered", problems);
        Assert.Contains("not in the reference: Probes unique index (Name)", problems);
        Assert.Equal(3, problems.Count);
    }

    [Fact]
    public void ShapeDifferences_ListedFilterThatNoIndexOfTheReferenceHas_Fails()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false);

        var problems = ModelConformance.ShapeDifferences(reference.Model, reference.Model, new ModelAllowances([RankFilter], _noCascades, _noUniques));

        Assert.Equal(["allowance for nothing: no index of the reference has the filter " + RankFilter], problems);
    }

    [Fact]
    public void ShapeDifferences_ListedFilterThatTheEngineKeeps_Fails()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false);

        var problems = ModelConformance.ShapeDifferences(reference.Model, reference.Model, new ModelAllowances([RankFilter], _noCascades, _noUniques));

        Assert.Equal(["allowance not used: expected Probes index (Rank) instead of Probes index (Rank), filtered"], problems);
    }

    [Fact]
    public void ShapeDifferences_IndexThatKeptItsFilter_MatchesWithoutAllowances()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: true);

        Assert.Empty(ModelConformance.ShapeDifferences(reference.Model, reference.Model, new ModelAllowances(_noFilters, _noCascades, _noUniques)));
    }

    [Fact]
    public void ShapeDifferences_OtherDifference_FailsWithTheAllowances()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false);
        using var actual = new ProbeContext(mySql: true, uniqueFiltered: false, rankIsUnique: true);

        var problems = ModelConformance.ShapeDifferences(reference.Model, actual.Model, new ModelAllowances([RankFilter], [_parentKey], _noUniques));

        Assert.Contains("missing: Probes index (Rank), filtered", problems);
        Assert.Contains("not in the reference: Probes unique index (Rank)", problems);
        Assert.Contains(problems, problem => problem.StartsWith("allowance not used: expected Probes foreign key (ParentId)", StringComparison.Ordinal));
    }

    [Fact]
    public void ShapeDifferences_CascadeThatDeletesNothingAndIsListed_IsAllowed()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false);
        using var actual = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false, parentDeletes: DeleteBehavior.NoAction);

        Assert.Empty(ModelConformance.ShapeDifferences(reference.Model, actual.Model, new ModelAllowances(_noFilters, [_parentKey], _noUniques)));
    }

    [Fact]
    public void ShapeDifferences_CascadeThatDeletesNothingAndIsNotListed_Fails()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false);
        using var actual = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false, parentDeletes: DeleteBehavior.NoAction);

        var problems = ModelConformance.ShapeDifferences(reference.Model, actual.Model, new ModelAllowances(_noFilters, _noCascades, _noUniques));

        Assert.Contains("missing: Probes foreign key (ParentId) -> Probes (Id), optional, on delete Cascade", problems);
        Assert.Contains("not in the reference: Probes foreign key (ParentId) -> Probes (Id), optional, on delete NoAction", problems);
        Assert.Equal(2, problems.Count);
    }

    [Fact]
    public void ShapeDifferences_CascadeOtherThanNoActionAndListed_Fails()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false);
        using var actual = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false, parentDeletes: DeleteBehavior.SetNull);

        var problems = ModelConformance.ShapeDifferences(reference.Model, actual.Model, new ModelAllowances(_noFilters, [_parentKey], _noUniques));

        Assert.Contains(problems, problem => problem.StartsWith("allowance not used", StringComparison.Ordinal));
        Assert.Contains("not in the reference: Probes foreign key (ParentId) -> Probes (Id), optional, on delete SetNull", problems);
    }

    [Fact]
    public void ShapeDifferences_ListedKeyThatDoesNotCascadeInTheReference_Fails()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false, parentDeletes: DeleteBehavior.NoAction);

        var problems = ModelConformance.ShapeDifferences(reference.Model, reference.Model, new ModelAllowances(_noFilters, [_parentKey], _noUniques));

        Assert.Equal(["allowance for a key that does not cascade: Probes foreign key (ParentId) -> Probes (Id), optional, on delete NoAction"], problems);
    }

    [Fact]
    public void ShapeDifferences_ListedKeyThatTheReferenceDoesNotHave_Fails()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false);

        var problems = ModelConformance.ShapeDifferences(reference.Model, reference.Model, new ModelAllowances(_noFilters, [("Probes", "Rank")], _noUniques));

        Assert.Equal(["allowance for nothing: the reference has no foreign key Probes (Rank)"], problems);
    }

    [Fact]
    public void ShapeDifferences_ListedCascadeThatTheEngineKeeps_Fails()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false);

        var problems = ModelConformance.ShapeDifferences(reference.Model, reference.Model, new ModelAllowances(_noFilters, [_parentKey], _noUniques));

        Assert.Equal(
            ["allowance not used: expected Probes foreign key (ParentId) -> Probes (Id), optional, on delete NoAction instead of Probes foreign key (ParentId) -> Probes (Id), optional, on delete Cascade"],
            problems);
    }

    [Fact]
    public void ShapeDifferences_UniqueIndexFilteredToTheRowsWithAValueAndListed_IsAllowed()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false);
        using var actual = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false, uniqueFilter: NullNameFilter);

        Assert.Empty(ModelConformance.ShapeDifferences(reference.Model, actual.Model, new ModelAllowances(_noFilters, _noCascades, [_nameNotNull])));
    }

    [Fact]
    public void ShapeDifferences_UniqueIndexFilteredToTheRowsWithAValueAndNotListed_Fails()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false);
        using var actual = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false, uniqueFilter: NullNameFilter);

        var problems = ModelConformance.ShapeDifferences(reference.Model, actual.Model, ModelAllowances.None);

        Assert.Equal(["missing: Probes unique index (Name)", "not in the reference: Probes unique index (Name), filtered"], problems);
    }

    [Fact]
    public void ShapeDifferences_UniqueIndexWithAnotherFilterThanTheListedOne_Fails()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false);
        using var actual = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false, uniqueFilter: "[Name] > N'm'");

        var problems = ModelConformance.ShapeDifferences(reference.Model, actual.Model, new ModelAllowances(_noFilters, _noCascades, [_nameNotNull]));

        Assert.Equal(["allowance for another filter: Probes unique index (Name), filtered has the filter [Name] > N'm', not [Name] IS NOT NULL"], problems);
    }

    [Fact]
    public void ShapeDifferences_ListedUniqueIndexThatTheEngineLeavesUnfiltered_Fails()
    {
        using var reference = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false);

        var problems = ModelConformance.ShapeDifferences(reference.Model, reference.Model, new ModelAllowances(_noFilters, _noCascades, [_nameNotNull]));

        Assert.Equal(["allowance not used: expected Probes unique index (Name), filtered instead of Probes unique index (Name)"], problems);
    }

    [Fact]
    public void ShapeDifferences_ListedIndexThatIsNotUniqueOrIsFilteredOrHasNoColumnThatTakesNull_Fails()
    {
        using var filtered = new ProbeContext(mySql: false, uniqueFiltered: true, nonUniqueFiltered: false);
        using var notUnique = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false);
        using var notNull = new ProbeContext(mySql: false, uniqueFiltered: false, nonUniqueFiltered: false, rankIsUnique: true);

        var onFiltered = ModelConformance.ShapeDifferences(filtered.Model, filtered.Model, new ModelAllowances(_noFilters, _noCascades, [_nameNotNull]));
        var onNotUnique = ModelConformance.ShapeDifferences(notUnique.Model, notUnique.Model, new ModelAllowances(_noFilters, _noCascades, [("Probes", "Rank", "[Rank] IS NOT NULL")]));
        var onMissing = ModelConformance.ShapeDifferences(notUnique.Model, notUnique.Model, new ModelAllowances(_noFilters, _noCascades, [("Probes", "Id", "[Id] IS NOT NULL")]));
        var onNotNull = ModelConformance.ShapeDifferences(notNull.Model, notNull.Model, new ModelAllowances(_noFilters, _noCascades, [("Probes", "Rank", "[Rank] IS NOT NULL")]));

        Assert.Equal(["allowance for an index that is not unique, is filtered, or has no column that takes NULL: Probes unique index (Name), filtered"], onFiltered);
        Assert.Equal(["allowance for an index that is not unique, is filtered, or has no column that takes NULL: Probes index (Rank)"], onNotUnique);
        Assert.Equal(["allowance for nothing: the reference has no index Probes (Id)"], onMissing);
        Assert.Equal(["allowance for an index that is not unique, is filtered, or has no column that takes NULL: Probes unique index (Rank)"], onNotNull);
    }

    private sealed class Probe
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public int Rank { get; set; }

        public int? ParentId { get; set; }

        public Probe? Parent { get; set; }
    }

    // A model with a unique and a non-unique index on one table, each filtered or not, and a relationship of the table
    // to itself, built by SQLite or by the provider of MySQL and MariaDB as it builds the model of Jellyfin.
    private sealed class ProbeContext(
        bool mySql,
        bool uniqueFiltered,
        bool nonUniqueFiltered = true,
        bool rankIsUnique = false,
        DeleteBehavior parentDeletes = DeleteBehavior.Cascade,
        string? uniqueFilter = null) : DbContext
    {
        private readonly MySqlDatabaseProvider _provider = new(NullLogger<MySqlDatabaseProvider>.Instance);

        public (bool MySql, bool UniqueFiltered, bool NonUniqueFiltered, bool RankIsUnique, DeleteBehavior ParentDeletes, string? UniqueFilter) Key
            => (mySql, uniqueFiltered, nonUniqueFiltered, rankIsUnique, parentDeletes, uniqueFilter);

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

            // Every variant builds a model of its own, rather than the one EF Core cached for the type, and stays out of the
            // cache of internal service providers of the process, as the contexts of the offline tests do.
            optionsBuilder.ReplaceService<IModelCacheKeyFactory, ProbeModelCacheKeyFactory>()
                .EnableServiceProviderCaching(false);
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
                    unique.HasFilter(NameFilter);
                }
                else if (uniqueFilter is not null)
                {
                    unique.HasFilter(uniqueFilter);
                }

                var rank = b.HasIndex(e => e.Rank).IsUnique(rankIsUnique);
                if (nonUniqueFiltered)
                {
                    rank.HasFilter(RankFilter);
                }

                b.HasOne(e => e.Parent).WithMany().HasForeignKey(e => e.ParentId).OnDelete(parentDeletes);
            });
        }
    }

    private sealed class ProbeModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) => (context.GetType(), ((ProbeContext)context).Key, designTime);
    }
}
