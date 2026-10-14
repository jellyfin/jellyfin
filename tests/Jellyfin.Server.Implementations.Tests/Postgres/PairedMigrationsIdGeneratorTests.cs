using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Jellyfin.Database.Providers.Postgres.Migrations;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Postgres;

public sealed partial class PairedMigrationsIdGeneratorTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("paired-migration-ids").FullName;
    private readonly RecordingLogger _logger = new();

    [GeneratedRegex(@"^\d{14}_[A-Za-z_]+$")]
    private static partial Regex FreshIdPattern();

    [Fact]
    public void GenerateId_NameOfAnExistingMigration_ReusesItsId()
    {
        CreateMigration("20261001090000_AddThing");
        CreateMigration("20261002090000_AddOtherThing");

        Assert.Equal("20261001090000_AddThing", new PairedMigrationsIdGenerator(_directory, _logger).GenerateId("AddThing"));
        Assert.Empty(_logger.Warnings);
    }

    [Fact]
    public void GenerateId_DesignerFileOnly_GeneratesAFreshId()
    {
        File.WriteAllText(Path.Combine(_directory, "20261001090000_AddThing.Designer.cs"), string.Empty);

        var id = new PairedMigrationsIdGenerator(_directory, _logger).GenerateId("AddThing");

        Assert.Matches(FreshIdPattern(), id);
    }

    [Fact]
    public void GenerateId_NameWithoutAMigration_GeneratesAFreshId()
    {
        CreateMigration("20261001090000_AddOtherThing");

        var id = new PairedMigrationsIdGenerator(_directory, _logger).GenerateId("AddThing");

        Assert.Matches(FreshIdPattern(), id);
        Assert.Empty(_logger.Warnings);
    }

    [Fact]
    public void GenerateId_NameThatIsTheTailOfAnotherName_GeneratesAFreshId()
    {
        CreateMigration("20261001090000_Add_Thing");

        var id = new PairedMigrationsIdGenerator(_directory, _logger).GenerateId("Thing");

        Assert.EndsWith("_Thing", id, StringComparison.Ordinal);
        Assert.NotEqual("20261001090000_Thing", id);
        Assert.Matches(FreshIdPattern(), id);
    }

    [Fact]
    public void GenerateId_MissingDirectory_GeneratesAFreshIdAndWarns()
    {
        var id = new PairedMigrationsIdGenerator(Path.Combine(_directory, "missing"), _logger).GenerateId("AddThing");

        Assert.Matches(FreshIdPattern(), id);
        var warning = Assert.Single(_logger.Warnings);
        Assert.Contains("'AddThing'", warning, StringComparison.Ordinal);
        Assert.Contains("will not share an id", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateId_NameThatDiffersInCase_GeneratesAFreshId()
    {
        CreateMigration("20261001090000_addthing");

        var id = new PairedMigrationsIdGenerator(_directory, _logger).GenerateId("AddThing");

        Assert.Matches(FreshIdPattern(), id);
        Assert.EndsWith("_AddThing", id, StringComparison.Ordinal);
        Assert.Empty(_logger.Warnings);
    }

    [Fact]
    public void GenerateId_WithoutADirectory_GeneratesAFreshIdAndWarns()
    {
        var id = new PairedMigrationsIdGenerator(null, _logger).GenerateId("AddThing");

        Assert.Matches(FreshIdPattern(), id);
        Assert.Single(_logger.Warnings);
    }

    [Fact]
    public void GenerateId_SeveralMigrationsWithTheName_GeneratesAFreshIdAndWarns()
    {
        CreateMigration("20261001090000_AddThing");
        CreateMigration("20261002090000_AddThing");

        var id = new PairedMigrationsIdGenerator(_directory, _logger).GenerateId("AddThing");

        Assert.Matches(FreshIdPattern(), id);
        var warning = Assert.Single(_logger.Warnings);
        Assert.Contains("More than one migration", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateId_TakesTheIdFromTheFileNameAndNotFromItsContent()
    {
        File.WriteAllText(
            Path.Combine(_directory, "20261001090000_AddThing.cs"),
            "[Migration(\"20990101000000_AddThing\")] public partial class AddThing { }");

        Assert.Equal("20261001090000_AddThing", new PairedMigrationsIdGenerator(_directory, _logger).GenerateId("AddThing"));
    }

    [Fact]
    public void GenerateId_FreshIds_AreStrictlyIncreasing()
    {
        var generator = new PairedMigrationsIdGenerator(_directory, _logger);

        var first = generator.GenerateId("AddThing");
        var second = generator.GenerateId("AddThing");

        Assert.True(string.CompareOrdinal(first, second) < 0, first + " " + second);
    }

    [Fact]
    public void GetNameAndIsValidId_AcceptTheGeneratedIds()
    {
        var generator = new PairedMigrationsIdGenerator(_directory, _logger);
        var id = generator.GenerateId("AddThing");

        Assert.True(generator.IsValidId(id));
        Assert.Equal("AddThing", generator.GetName(id));
        Assert.False(generator.IsValidId("AddThing"));
        Assert.False(generator.IsValidId("2026100109000_AddThing"));
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private void CreateMigration(string id)
    {
        File.WriteAllText(Path.Combine(_directory, id + ".cs"), string.Empty);
        File.WriteAllText(Path.Combine(_directory, id + ".Designer.cs"), string.Empty);
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                Warnings.Add(formatter(state, exception));
            }
            else
            {
                Assert.Fail($"Unexpected {logLevel}: {formatter(state, exception)}");
            }
        }
    }
}
