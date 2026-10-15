using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Server.Implementations.Tests.DatabaseInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.MySql;

/// <summary>
/// Every text column of MySQL and MariaDB compares as binary: the model gives each column whose type in the database is
/// text the collation <c>utf8mb4_0900_bin</c>, whatever the type of its property, and makes it the default of the
/// database and of every table. Nothing here connects to a server.
/// </summary>
public partial class MySqlCollationModelTests
{
    private const string Binary = "utf8mb4_0900_bin";

    private enum ProbeKind
    {
        First,
        Second
    }

    [Theory]
    [InlineData("8.4-mysql")]
    [InlineData("9.7.0-mysql")]
    [InlineData("11.4.7-mariadb")]
    [InlineData("11.8.0-mariadb")]
    [InlineData("12.3.0-mariadb")]
    public void TextColumns_HaveTheBinaryCollation(string server)
    {
        using var context = OfflineContexts.MySql(server);

        var text = Properties(context).Where(p => IsTextStoreType(p.GetColumnType())).ToList();

        Assert.Contains(text, p => p.DeclaringType.ClrType.Name == "KeyframeData" && p.Name == "KeyframeTicks");
        Assert.All(text, p => Assert.True(p.GetCollation() == Binary, $"{p.DeclaringType.DisplayName()}.{p.Name} ({p.GetColumnType()}) has the collation '{p.GetCollation()}'."));
    }

    [Theory]
    [InlineData("8.4-mysql")]
    [InlineData("11.4.7-mariadb")]
    public void PropertiesStoredAsStrings_HaveATextType(string server)
    {
        using var context = OfflineContexts.MySql(server);

        // A string type of the database that is not text, such as json, takes no collation and would need a decision.
        var strings = Properties(context).Where(p => ProviderClrType(p) == typeof(string)).ToList();

        Assert.NotEmpty(strings);
        Assert.All(strings, p => Assert.True(IsTextStoreType(p.GetColumnType()), $"{p.DeclaringType.DisplayName()}.{p.Name} is stored as {p.GetColumnType()}."));
    }

    [Fact]
    public void Model_MakesTheBinaryCollationTheDefaultOfTheDatabaseAndTheTables()
    {
        using var context = OfflineContexts.MySql();
        var model = context.GetService<IDesignTimeModel>().Model;

        Assert.Equal(Binary, model.GetCollation());
        Assert.Equal("utf8mb4", model.GetCharSet());
        Assert.Equal(DelegationModes.ApplyToAll, model.GetActualCollationDelegation());
    }

    [Fact]
    public void Baseline_MakesTheBinaryCollationTheDefaultOfTheDatabase()
    {
        using var context = OfflineContexts.MySql();
        var migrations = context.GetService<IMigrationsAssembly>();
        var baseline = migrations.CreateMigration(migrations.Migrations[MySqlMigrationBaseline.MigrationId], context.Database.ProviderName!);

        var alter = Assert.IsType<AlterDatabaseOperation>(baseline.UpOperations[0]);

        Assert.Equal(Binary, alter.Collation);
        Assert.Single(baseline.UpOperations.OfType<AlterDatabaseOperation>());
    }

    [Theory]
    [InlineData("8.4-mysql")]
    [InlineData("11.4.7-mariadb")]
    public void BaselineScript_GivesTheDatabaseEveryTableAndEveryTextColumnTheBinaryCollation(string server)
    {
        using var context = OfflineContexts.MySql(server);

        // The script starts with the history table, which EF Core creates before it applies the first migration.
        var script = context.GetService<IMigrator>().GenerateScript(toMigration: MySqlMigrationBaseline.MigrationId);
        var tables = CreateTable().Matches(script).Select(m => m.Value).ToList();

        Assert.Contains($"ALTER DATABASE COLLATE {Binary};", script, StringComparison.Ordinal);
        Assert.DoesNotContain("ALTER DATABASE CHARACTER SET", script, StringComparison.Ordinal);
        Assert.Contains(tables, t => t.Contains("`__EFMigrationsHistory`", StringComparison.Ordinal));
        Assert.Contains(tables, t => t.Contains("`KeyframeTicks` longtext", StringComparison.Ordinal));
        Assert.All(tables, t => Assert.EndsWith($"CHARACTER SET=utf8mb4 COLLATE={Binary};", t, StringComparison.Ordinal));
        Assert.All(tables, t => Assert.Empty(CharSetWithoutBinaryCollation().Matches(t)));
    }

    [Fact]
    public void HistoryTable_HasTheBinaryCollation()
    {
        using var context = OfflineContexts.MySql();

        var script = context.GetService<IHistoryRepository>().GetCreateScript();

        Assert.Contains($"`MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE {Binary} NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains($"`ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE {Binary} NOT NULL", script, StringComparison.Ordinal);
        Assert.Contains($"CHARACTER SET=utf8mb4 COLLATE={Binary};", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Convention_ChoosesTheColumnsByTheirTypeInTheDatabase()
    {
        using var context = new ProbeContext();
        var entityType = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Probe))!;

        string? Collation(string property) => entityType.FindProperty(property)!.GetCollation();

        Assert.Multiple(
            () => Assert.Equal(Binary, Collation(nameof(Probe.Text))),
            () => Assert.Equal(Binary, Collation(nameof(Probe.BoundedText))),
            () => Assert.Equal(Binary, Collation(nameof(Probe.KindAsText))),
            () => Assert.Equal(Binary, Collation(nameof(Probe.Link))),
            () => Assert.Equal(Binary, Collation(nameof(Probe.Names))),
            () => Assert.Equal(Binary, Collation(nameof(Probe.Numbers))),
            () => Assert.Null(Collation(nameof(Probe.Id))),
            () => Assert.Null(Collation(nameof(Probe.KindAsNumber))),
            () => Assert.Null(Collation(nameof(Probe.Bytes))),
            () => Assert.Null(Collation(nameof(Probe.Key))),
            () => Assert.Null(Collation(nameof(Probe.Document))));
    }

    // Written apart from the convention, so that the test does not take the convention's word for what text is.
    private static bool IsTextStoreType(string? storeType)
        => storeType is not null && TextStoreType().IsMatch(storeType);

    private static Type ProviderClrType(IProperty property)
    {
        var mapping = property.GetTypeMapping();
        return mapping.Converter?.ProviderClrType ?? mapping.ClrType;
    }

    private static IEnumerable<IProperty> Properties(JellyfinDbContext context)
    {
        return context.GetService<IDesignTimeModel>().Model.GetEntityTypes()
            .Where(e => e.GetTableName() is not null)
            .SelectMany(e => e.GetFlattenedProperties());
    }

    [GeneratedRegex(@"^(char|varchar|tinytext|text|mediumtext|longtext)(\(|\s|$)", RegexOptions.IgnoreCase)]
    private static partial Regex TextStoreType();

    [GeneratedRegex(@"CREATE TABLE[^;]*;")]
    private static partial Regex CreateTable();

    // A column that names its character set and no collation gets the default collation of that character set.
    [GeneratedRegex(@"CHARACTER SET utf8mb4(?! COLLATE utf8mb4_0900_bin)")]
    private static partial Regex CharSetWithoutBinaryCollation();

    private sealed class Probe
    {
        public int Id { get; set; }

        public string? Text { get; set; }

        public string? BoundedText { get; set; }

        public ProbeKind KindAsText { get; set; }

        public ProbeKind KindAsNumber { get; set; }

        public Uri? Link { get; set; }

        public List<string>? Names { get; set; }

        public List<long>? Numbers { get; set; }

        public byte[]? Bytes { get; set; }

        public Guid Key { get; set; }

        public string? Document { get; set; }
    }

    // A model with a column of each kind, built by the provider as it builds the model of Jellyfin.
    private sealed class ProbeContext : DbContext
    {
        private readonly MySqlDatabaseProvider _provider = new(NullLogger<MySqlDatabaseProvider>.Instance);

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _provider.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Probe>(b =>
            {
                b.Property(e => e.BoundedText).HasMaxLength(64);
                b.Property(e => e.KindAsText).HasConversion<string>();
                b.Property(e => e.Document).HasColumnType("json");
            });
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            _provider.ConfigureConventions(configurationBuilder);
        }
    }
}
