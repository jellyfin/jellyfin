using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Extensions;
using Jellyfin.Server.Migrations.Routines;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests.Migrations;

/// <summary>
/// Runs the merger against a real SQLite database so the queries are actually translated.
/// </summary>
public sealed class DuplicatePeopleMergerTests : IDisposable
{
    private const string PersonType = "MediaBrowser.Controller.Entities.Person";
    private static readonly Guid _movieId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly DuplicatePeopleMerger _merger;

    public DuplicatePeopleMergerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>().UseSqlite(_connection).Options;

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();
            ctx.BaseItems.Add(new BaseItemEntity { Id = _movieId, Type = "MediaBrowser.Controller.Entities.Movies.Movie", Name = "Movie" });
            ctx.SaveChanges();
        }

        _merger = new DuplicatePeopleMerger(
            NullLogger.Instance,
            new Mock<ILibraryManager>().Object,
            new Mock<IItemPersistenceService>().Object);
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task MergePeoplesRowsAsync_SpellingOnlyDuplicates_KeepTheMostCreditedRow()
    {
        var keeper = AddPerson("Zoe Saldana", credits: 2);
        var duplicate = AddPerson("Zoe Saldaña", credits: 1);

        await _merger.MergePeoplesRowsAsync(CreateDbContext(), n => n.GetCleanValue(), "spelling-only", CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal(keeper, ctx.Peoples.Single().Id);
        // Every credit survives the merge, repointed at the keeper.
        Assert.Equal(3, ctx.PeopleBaseItemMap.Count(m => m.PeopleId.Equals(keeper)));
        Assert.Empty(ctx.PeopleBaseItemMap.Where(m => m.PeopleId.Equals(duplicate)));
    }

    [Fact]
    public async Task MergePeoplesRowsAsync_CreditsThatWouldCollide_AreDroppedNotDuplicated()
    {
        var keeper = AddPerson("Zoe Saldana", credits: 0);
        var duplicate = AddPerson("Zoe Saldaña", credits: 0);
        AddCredit(keeper, "Neytiri");
        AddCredit(duplicate, "Neytiri");

        await _merger.MergePeoplesRowsAsync(CreateDbContext(), n => n.GetCleanValue(), "spelling-only", CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Single(ctx.Peoples);
        Assert.Single(ctx.PeopleBaseItemMap);
    }

    [Fact]
    public async Task MergePeoplesRowsAsync_DifferentPersonTypes_AreLeftAlone()
    {
        AddPerson("Zoe Saldana", credits: 0, personType: "Actor");
        AddPerson("Zoe Saldaña", credits: 0, personType: "Director");

        await _merger.MergePeoplesRowsAsync(CreateDbContext(), n => n.GetCleanValue(), "spelling-only", CancellationToken.None);

        using var ctx = CreateDbContext();
        Assert.Equal(2, ctx.Peoples.Count());
    }

    [Fact]
    public async Task MergePersonBaseItemsAsync_SpellingOnlyDuplicates_AreGrouped()
    {
        AddPersonItem("Zoe Saldana");
        AddPersonItem("Zoe Saldaña");
        AddPersonItem("Someone Else");

        // The library manager cannot resolve the stubs, so deletion falls through to the persistence
        // service; assert on what it was asked to remove.
        var persistence = new Mock<IItemPersistenceService>();
        var merger = new DuplicatePeopleMerger(NullLogger.Instance, new Mock<ILibraryManager>().Object, persistence.Object);

        await merger.MergePersonBaseItemsAsync(CreateDbContext(), n => n.GetCleanValue(), "spelling-only", CancellationToken.None);

        // One of the two spellings is removed, the unrelated person is untouched.
        persistence.Verify(p => p.DeleteItem(It.Is<IReadOnlyList<Guid>>(ids => ids.Count == 1)), Times.Once);
    }

    private Guid AddPerson(string name, int credits, string personType = "Actor")
    {
        using var ctx = CreateDbContext();
        var id = Guid.NewGuid();
        ctx.Peoples.Add(new People { Id = id, Name = name, CleanName = name.GetCleanValue(), PersonType = personType });
        for (var i = 0; i < credits; i++)
        {
            ctx.PeopleBaseItemMap.Add(new PeopleBaseItemMap { Item = null!, ItemId = _movieId, People = null!, PeopleId = id, Role = $"{name}-{i}" });
        }

        ctx.SaveChanges();
        return id;
    }

    private void AddCredit(Guid personId, string role)
    {
        using var ctx = CreateDbContext();
        ctx.PeopleBaseItemMap.Add(new PeopleBaseItemMap { Item = null!, ItemId = _movieId, People = null!, PeopleId = personId, Role = role });
        ctx.SaveChanges();
    }

    private void AddPersonItem(string name)
    {
        using var ctx = CreateDbContext();
        ctx.BaseItems.Add(new BaseItemEntity { Id = Guid.NewGuid(), Type = PersonType, Name = name, CleanName = name.GetCleanValue() });
        ctx.SaveChanges();
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
