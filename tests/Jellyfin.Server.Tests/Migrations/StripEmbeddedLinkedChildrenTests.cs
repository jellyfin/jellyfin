using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Extensions.Json;
using Jellyfin.Server.Implementations.Item;
using Jellyfin.Server.Migrations.Routines;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

public sealed class StripEmbeddedLinkedChildrenTests : IDisposable
{
    private const string DeadKeysAtStart = """{"LinkedChildren":[{"Path":"/media/Extras/Making of.mkv","Type":"Manual"}],"ExtraIds":["4c9f6b0e2a1d4e8f9b3c5d7e1f2a3b4c"],""";
    private const string DeadKeysAtEnd = ""","SupportsExternalTransfer":false}""";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;

    public StripEmbeddedLinkedChildrenTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = CreateDbContext();
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public void Perform_TopLevelDeadKeys_RemovesOnlyThem()
    {
        var movie = new Movie
        {
            Id = Id(1),
            AdditionalParts = ["/media/Movies/Amélie & <Nino> (2001)/Amélie - part 2.mkv"],
            RemoteTrailers = [new MediaUrl { Name = "Bande-annonce « officielle »", Url = "https://example.com/watch?v=1&lang=fr" }],
            Size = 4123456789
        };
        var serialized = JsonSerializer.Serialize(movie, JsonDefaults.Options);
        Assert.Contains("\\u00E9", serialized, StringComparison.Ordinal);
        Seed((movie.Id, DeadKeysAtStart + serialized[1..^1] + DeadKeysAtEnd));

        Perform();

        Assert.Equal(serialized, GetData(movie.Id));
    }

    [Fact]
    public void Perform_NestedDeadKeys_LeavesDataAlone()
    {
        const string Data = """{"Name":"Amélie","Owner":{"LinkedChildren":[],"ExtraIds":[]},"Tags":["SupportsExternalTransfer"]}""";
        Seed((Id(1), Data));

        Perform();

        Assert.Equal(Data, GetData(Id(1)));
    }

    [Fact]
    public void Perform_MalformedData_LeavesItAloneAndFixesTheRest()
    {
        const string Malformed = """{"LinkedChildren":[],"Name":"Cut off""";
        Seed((Id(1), Malformed), (Id(2), DeadKeysAtStart + "\"Name\":\"Fine\"" + DeadKeysAtEnd));

        Perform();

        Assert.Equal(Malformed, GetData(Id(1)));
        Assert.Equal("""{"Name":"Fine"}""", GetData(Id(2)));
    }

    [Fact]
    public void Perform_DataWithoutDeadKeys_LeavesItAlone()
    {
        const string Data = """{ "Name": "Plain", "Overview": "Linked children, extra ids and external transfers." }""";
        Seed((Id(1), Data), (Id(2), null));

        Perform();

        Assert.Equal(Data, GetData(Id(1)));
        Assert.Null(GetData(Id(2)));
    }

    [Fact]
    public void Perform_MoreRowsThanOneBatch_PagesPastRowsLeftAlone()
    {
        const string Nested = """{"Name":"Nested","Owner":{"ExtraIds":[]}}""";

        // The rows that come first still match after the run, so they fill more than a batch on their own.
        var rows = Enumerable.Range(0, 2500)
            .Select(i => (Id(i), (string?)(i < 1200 ? Nested : DeadKeysAtStart + "\"Name\":\"Item\"" + DeadKeysAtEnd)))
            .ToArray();
        Seed(rows);

        Perform();

        using var context = CreateDbContext();
        var data = context.BaseItems.AsNoTracking()
            .Where(e => !e.Id.Equals(BaseItemRepository.PlaceholderId))
            .OrderBy(e => e.Id)
            .Select(e => e.Data)
            .ToList();
        Assert.Equal(2500, data.Count);
        Assert.All(data.Take(1200), d => Assert.Equal(Nested, d));
        Assert.All(data.Skip(1200), d => Assert.Equal("""{"Name":"Item"}""", d));
    }

    private static Guid Id(int index) => Guid.Parse("10000000-0000-0000-0000-" + index.ToString("D12", CultureInfo.InvariantCulture));

    private void Seed(params (Guid Id, string? Data)[] rows)
    {
        using var context = CreateDbContext();
        context.BaseItems.AddRange(rows.Select(row => new BaseItemEntity { Id = row.Id, Type = typeof(Movie).FullName!, Data = row.Data }));
        context.SaveChanges();
    }

    private string? GetData(Guid id)
    {
        using var context = CreateDbContext();
        return context.BaseItems.AsNoTracking().Where(e => e.Id.Equals(id)).Select(e => e.Data).Single();
    }

    private void Perform()
    {
        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        new StripEmbeddedLinkedChildren(NullLoggerFactory.Instance, factory.Object).Perform();
    }

    private JellyfinDbContext CreateDbContext() => new(
        _dbOptions,
        NullLogger<JellyfinDbContext>.Instance,
        new SqliteDatabaseProvider(new Mock<IApplicationPaths>().Object, NullLogger<SqliteDatabaseProvider>.Instance),
        new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
}
