using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

public class MigrationContentsTests
{
    private static readonly string[] Providers = ["SQLite", "PostgreSQL", "Other"];

    [Fact]
    [Trait("Category", "Migrations")]
    public void MigrationsThatShareAnId_HaveTheSameOperationsOrAnAllowlistEntry()
    {
        var migrations = MigrationProvider.All.SelectMany(MigrationContents.Load).ToList();
        var differences = MigrationContents.Compare(migrations);

        // The operations of the paired migrations, so that the output of a run shows what was compared.
        var output = TestContext.Current.TestOutputHelper;
        foreach (var migration in migrations.GroupBy(e => e.MigrationId).Where(e => e.Count() > 1).SelectMany(e => e))
        {
            output?.WriteLine($"{migration.MigrationId} {migration.Provider}");
            output?.WriteLine("  Up:   " + string.Join(Environment.NewLine + "        ", migration.Up.SelectMany(MigrationContents.Describe)));
            output?.WriteLine("  Down: " + string.Join(Environment.NewLine + "        ", migration.Down.SelectMany(MigrationContents.Describe)));
        }

        output?.WriteLine($"{differences.Count} difference(s):");
        foreach (var difference in differences)
        {
            output?.WriteLine($"  {difference.MigrationId} {difference.Direction} {difference.Provider}: {difference.Operation}");
        }

        var problems = MigrationContentsAllowlist.Check(differences, MigrationContentsAllowlist.Load(), MigrationProvider.All.Select(e => e.Name).ToList());

        Assert.True(problems is null, problems);
    }

    [Fact]
    public void Compare_SameOperationsInAnotherOrderAndWithOtherAnnotations_FindsNoDifference()
    {
        var sqlite = new AddColumnOperation { Table = "Items", Name = "Title", ColumnType = "TEXT" };
        var postgres = new AddColumnOperation { Table = "Items", Name = "Title", ColumnType = "text", Collation = "C" };
        postgres.AddAnnotation("Npgsql:Something", true);

        var differences = MigrationContents.Compare(
        [
            Migration("SQLite", up: [sqlite, Index("Items", "Title")]),
            Migration("PostgreSQL", up: [Index("Items", "Title"), postgres])
        ]);

        Assert.Empty(differences);
    }

    [Fact]
    public void Compare_SqlOnOneProviderOnly_IsADifferenceOfThatProvider()
    {
        var differences = MigrationContents.Compare(
        [
            Migration("SQLite", up: [new SqlOperation { Sql = "DELETE FROM \"Items\";" }]),
            Migration("PostgreSQL")
        ]);

        var difference = Assert.Single(differences);
        Assert.Equal(new MigrationDifference("20990101000000_Change", MigrationContents.Up, "SQLite", MigrationContents.DescribeSql("DELETE FROM \"Items\";")), difference);
    }

    [Fact]
    public void Compare_DifferentSqlText_IsADifferenceOfEachProvider()
    {
        var differences = MigrationContents.Compare(
        [
            Migration("SQLite", down: [new SqlOperation { Sql = "DROP INDEX \"A\";" }]),
            Migration("PostgreSQL", down: [new SqlOperation { Sql = "DROP INDEX \"B\";" }])
        ]);

        Assert.Equal(["PostgreSQL", "SQLite"], differences.Select(e => e.Provider).Order(StringComparer.Ordinal));
        Assert.All(differences, e => Assert.Equal(MigrationContents.Down, e.Direction));
    }

    [Fact]
    public void Compare_OperationThatOneOfThreeProvidersLacks_IsOneDifferenceOfTheOtherTwo()
    {
        var differences = MigrationContents.Compare(
        [
            Migration("SQLite"),
            Migration("PostgreSQL", up: [new DropColumnOperation { Table = "Items", Name = "Old" }]),
            Migration("Other", up: [new DropColumnOperation { Table = "Items", Name = "Old" }])
        ]);

        Assert.Equal(new MigrationDifference("20990101000000_Change", MigrationContents.Up, "PostgreSQL, Other", "DropColumn Items.Old"), Assert.Single(differences));
    }

    [Fact]
    public void Compare_OperationRepeated_CountsTheExtraTimes()
    {
        var sql = new SqlOperation { Sql = "SELECT 1;" };

        var differences = MigrationContents.Compare(
        [
            Migration("SQLite", up: [sql, sql, sql]),
            Migration("PostgreSQL", up: [sql])
        ]);

        Assert.Equal(MigrationContents.DescribeSql("SELECT 1;") + " (x2)", Assert.Single(differences).Operation);
    }

    [Fact]
    public void Compare_MigrationOfOneProviderOnly_IsNotCompared()
    {
        var differences = MigrationContents.Compare(
        [
            Migration("SQLite", up: [new SqlOperation { Sql = "SELECT 1;" }]),
            new MigrationOperations("PostgreSQL", "20200101000000_InitialCreate", [new SqlOperation { Sql = "SELECT 2;" }], [])
        ]);

        Assert.Empty(differences);
    }

    [Fact]
    public void Describe_CreateTable_NamesEachColumnAndKey()
    {
        var table = new CreateTableOperation
        {
            Name = "Items",
            Columns = { new AddColumnOperation { Name = "Id" }, new AddColumnOperation { Name = "ParentId", IsNullable = true } },
            PrimaryKey = new AddPrimaryKeyOperation { Columns = ["Id"] },
            ForeignKeys = { new AddForeignKeyOperation { Columns = ["ParentId"], PrincipalTable = "Items", PrincipalColumns = ["Id"], OnDelete = ReferentialAction.Cascade } }
        };

        Assert.Equal(
            [
                "CreateTable Items",
                "CreateTable Items column Id not null",
                "CreateTable Items column ParentId null",
                "CreateTable Items primary key (Id)",
                "CreateTable Items foreign key (ParentId) -> Items (Id) on delete Cascade"
            ],
            MigrationContents.Describe(table));
    }

    [Fact]
    public void Describe_Index_ComparesColumnsAndNotTheName()
    {
        Assert.Equal(
            MigrationContents.Describe(new CreateIndexOperation { Name = "IX_Items_ParentId_Name", Table = "Items", Columns = ["ParentId", "Name"] }),
            MigrationContents.Describe(new CreateIndexOperation { Name = "IX_Items_ParentId_Na~", Table = "Items", Columns = ["ParentId", "Name"] }));
    }

    [Fact]
    public void DescribeSql_IgnoresRunsOfWhiteSpace_AndKeepsEverythingElse()
    {
        Assert.Equal(MigrationContents.DescribeSql("DROP  INDEX\n\t\"A\";  "), MigrationContents.DescribeSql("DROP INDEX \"A\";"));
        Assert.NotEqual(MigrationContents.DescribeSql("DROP INDEX \"a\";"), MigrationContents.DescribeSql("DROP INDEX \"A\";"));
        Assert.StartsWith("Sql ", MigrationContents.DescribeSql(new string('x', 500)), StringComparison.Ordinal);
        Assert.EndsWith("...", MigrationContents.DescribeSql(new string('x', 500)), StringComparison.Ordinal);
    }

    [Fact]
    public void Check_EveryDifferenceWithOneEntry_HasNoProblem()
    {
        var difference = new MigrationDifference("20990101000000_Change", MigrationContents.Down, "SQLite", "Sql 0123456789abcdef DROP INDEX \"A\";");

        Assert.Null(MigrationContentsAllowlist.Check([difference], [Entry(difference)], Providers));
    }

    [Fact]
    public void Check_DifferenceWithoutAnEntry_IsMissingAndNamesTheCommand()
    {
        var difference = new MigrationDifference("20990101000000_Change", MigrationContents.Up, "PostgreSQL", "AddColumn Items.Title");

        var problems = MigrationContentsAllowlist.Check([difference], [], Providers);

        Assert.NotNull(problems);
        Assert.Contains("Missing: 1 difference(s)", problems, StringComparison.Ordinal);
        Assert.Contains("\"operation\": \"AddColumn Items.Title\"", problems, StringComparison.Ordinal);
        Assert.Contains(MigrationProvider.Command, problems, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_EntryWithoutADifference_IsStaleAndNamesTheCommand()
    {
        var difference = new MigrationDifference("20990101000000_Change", MigrationContents.Up, "PostgreSQL", "AddColumn Items.Title");

        var problems = MigrationContentsAllowlist.Check([], [Entry(difference)], Providers);

        Assert.NotNull(problems);
        Assert.Contains("Stale: 1 entr(y/ies)", problems, StringComparison.Ordinal);
        Assert.Contains("20990101000000_Change Up PostgreSQL: AddColumn Items.Title", problems, StringComparison.Ordinal);
        Assert.Contains(MigrationProvider.Command, problems, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_EntryForAnotherDirectionOrProvider_DoesNotMatch()
    {
        var difference = new MigrationDifference("20990101000000_Change", MigrationContents.Up, "PostgreSQL", "AddColumn Items.Title");

        var problems = MigrationContentsAllowlist.Check(
            [difference],
            [Entry(difference with { Direction = MigrationContents.Down }), Entry(difference with { Provider = "SQLite" })],
            Providers);

        Assert.NotNull(problems);
        Assert.Contains("Missing: 1", problems, StringComparison.Ordinal);
        Assert.Contains("Stale: 2", problems, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_TwoEntriesForOneDifference_IsAmbiguousAndNamesTheCommand()
    {
        var difference = new MigrationDifference("20990101000000_Change", MigrationContents.Up, "PostgreSQL", "AddColumn Items.Title");

        var problems = MigrationContentsAllowlist.Check([difference], [Entry(difference), Entry(difference) with { Reason = "Another reason." }], Providers);

        Assert.NotNull(problems);
        Assert.Contains("Ambiguous: 1 difference(s)", problems, StringComparison.Ordinal);
        Assert.DoesNotContain("Missing", problems, StringComparison.Ordinal);
        Assert.Contains(MigrationProvider.Command, problems, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_EntryWithoutAReasonOrWithAnUnknownProvider_IsInvalid()
    {
        var difference = new MigrationDifference("20990101000000_Change", MigrationContents.Up, "PostgreSQL", "AddColumn Items.Title");
        var other = difference with { Provider = "SqlServer" };

        var problems = MigrationContentsAllowlist.Check([difference, other], [Entry(difference) with { Reason = " " }, Entry(other)], Providers);

        Assert.NotNull(problems);
        Assert.Contains("Invalid: 2", problems, StringComparison.Ordinal);
        Assert.Contains("The reason is missing", problems, StringComparison.Ordinal);
        Assert.Contains("The provider is not one of", problems, StringComparison.Ordinal);
    }

    [Fact]
    public void Compare_NullabilityUniquenessOrOnDeleteThatDiffers_IsADifference()
    {
        var differences = MigrationContents.Compare(
        [
            Migration("SQLite", up:
            [
                new AddColumnOperation { Table = "Items", Name = "Title", IsNullable = true },
                new CreateIndexOperation { Table = "Items", Columns = ["Title"], IsUnique = true },
                new AddForeignKeyOperation { Table = "Items", Columns = ["ParentId"], PrincipalTable = "Items", PrincipalColumns = ["Id"], OnDelete = ReferentialAction.Cascade }
            ]),
            Migration("PostgreSQL", up:
            [
                new AddColumnOperation { Table = "Items", Name = "Title", IsNullable = false },
                new CreateIndexOperation { Table = "Items", Columns = ["Title"], IsUnique = false },
                new AddForeignKeyOperation { Table = "Items", Columns = ["ParentId"], PrincipalTable = "Items", PrincipalColumns = ["Id"], OnDelete = ReferentialAction.NoAction }
            ])
        ]);

        Assert.Equal(
            [
                "PostgreSQL: AddColumn Items.Title not null",
                "PostgreSQL: AddForeignKey Items (ParentId) -> Items (Id) on delete NoAction",
                "PostgreSQL: CreateIndex Items (Title)",
                "SQLite: AddColumn Items.Title null",
                "SQLite: AddForeignKey Items (ParentId) -> Items (Id) on delete Cascade",
                "SQLite: CreateIndex Items (Title) unique"
            ],
            differences.Select(e => e.Provider + ": " + e.Operation).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Compare_OperationOfAKindWithoutItsOwnDescription_IsStillADifference()
    {
        var differences = MigrationContents.Compare(
        [
            Migration("SQLite"),
            Migration("PostgreSQL", up: [new AlterDatabaseOperation(), new EnsureSchemaOperation { Name = "jellyfin" }])
        ]);

        Assert.Equal(["AlterDatabase", "EnsureSchema jellyfin"], differences.Select(e => e.Operation).Order(StringComparer.Ordinal));
        Assert.All(differences, e => Assert.Equal("PostgreSQL", e.Provider));
    }

    [Fact]
    public void Check_EntryWithAnotherDirection_IsInvalid()
    {
        var difference = new MigrationDifference("20990101000000_Change", MigrationContents.Up, "PostgreSQL", "AddColumn Items.Title null");

        var problems = MigrationContentsAllowlist.Check([difference], [Entry(difference), Entry(difference) with { Direction = "up" }], Providers);

        Assert.NotNull(problems);
        Assert.Contains("Invalid: 1", problems, StringComparison.Ordinal);
        Assert.Contains("The direction is not Up or Down", problems, StringComparison.Ordinal);
        Assert.Contains(MigrationProvider.Command, problems, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_EntryWithACategoryThatIsNotDefined_IsInvalid()
    {
        var difference = new MigrationDifference("20990101000000_Change", MigrationContents.Up, "PostgreSQL", "AddColumn Items.Title null");

        var problems = MigrationContentsAllowlist.Check([difference], [Entry(difference) with { Category = (MigrationContentsCategory)99 }], Providers);

        Assert.NotNull(problems);
        Assert.Contains("The category is not known", problems, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("PostgreSQL")]
    [InlineData("SQLite, PostgreSQL")]
    public void Check_LegacySqliteDataOfAnotherProvider_IsInvalid(string provider)
    {
        var difference = new MigrationDifference("20990101000000_Change", MigrationContents.Up, provider, "Sql 0123456789abcdef UPDATE \"Items\" SET \"A\" = 1;");

        var problems = MigrationContentsAllowlist.Check([difference], [Entry(difference) with { Category = MigrationContentsCategory.LegacySqliteData }], Providers);

        Assert.NotNull(problems);
        Assert.Contains("Only the reference provider SQLite can have a LegacySqliteData difference", problems, StringComparison.Ordinal);
    }

    [Fact]
    public void Check_LegacySqliteDataOfTheReferenceProvider_IsValid()
    {
        var difference = new MigrationDifference("20990101000000_Change", MigrationContents.Up, "SQLite", "Sql 0123456789abcdef UPDATE \"Items\" SET \"A\" = 1;");

        Assert.Null(MigrationContentsAllowlist.Check([difference], [Entry(difference) with { Category = MigrationContentsCategory.LegacySqliteData }], Providers));
    }

    [Theory]
    [InlineData("""[{"migration": "a", "direction": "Up", "provider": "SQLite", "operation": "b", "category": "BugFix", "reason": "c"}]""")]
    [InlineData("""[{"migration": "a", "direction": "Up", "provider": "SQLite", "operation": "b", "category": "EngineSpecific"}]""")]
    [InlineData("""[{"migration": "a", "direction": "Up", "provider": "SQLite", "operation": "b", "category": "EngineSpecific", "reason": null}]""")]
    [InlineData("""[{"migration": "a", "direction": "Up", "provider": "SQLite", "operation": "b", "category": "EngineSpecific", "reason": "c", "note": "d"}]""")]
    [InlineData("""[{"migration": "a", "direction": "Up", "provider": "SQLite", "operation": "b", "category": 1, "reason": "c"}]""")]
    [InlineData("""{"migration": "a"}""")]
    [InlineData("null")]
    public void Parse_AnEntryThatCannotBeRead_FailsWithAMessageThatNamesTheCommand(string json)
    {
        var exception = Assert.Throws<InvalidDataException>(() => MigrationContentsAllowlist.Parse(json));

        Assert.Contains(MigrationProvider.Command, exception.Message, StringComparison.Ordinal);
        Assert.Contains(MigrationContentsAllowlist.RepositoryPath, exception.Message, StringComparison.Ordinal);
        Assert.Contains("The allowlist cannot be read", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_TheProposedEntryOfAMissingDifferencePastedUnchanged_FailsWithAMessageThatNamesTheCommand()
    {
        var problems = MigrationContentsAllowlist.Check([new MigrationDifference("20990101000000_Change", MigrationContents.Up, "PostgreSQL", "AddColumn Items.Title null")], [], Providers);
        Assert.NotNull(problems);
        var proposed = "[" + problems[problems.IndexOf('{', StringComparison.Ordinal)..(problems.LastIndexOf('}') + 1)] + "]";

        var exception = Assert.Throws<InvalidDataException>(() => MigrationContentsAllowlist.Parse(proposed));

        Assert.Contains(MigrationProvider.Command, exception.Message, StringComparison.Ordinal);
        Assert.Contains("Replace the placeholders of a proposed entry", exception.Message, StringComparison.Ordinal);
    }

    private static MigrationOperations Migration(string provider, IReadOnlyList<MigrationOperation>? up = null, IReadOnlyList<MigrationOperation>? down = null)
        => new(provider, "20990101000000_Change", up ?? [], down ?? []);

    private static CreateIndexOperation Index(string table, string column) => new() { Name = "IX_" + table + "_" + column, Table = table, Columns = [column] };

    private static MigrationContentsAllowlistEntry Entry(MigrationDifference difference)
        => new(difference.MigrationId, difference.Direction, difference.Provider, difference.Operation, MigrationContentsCategory.EngineSpecific, "A reason.");
}
