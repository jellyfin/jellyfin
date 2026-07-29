using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class PeopleRepositorySearchTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly PeopleRepository _repository;

    public PeopleRepositorySearchTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();
            ctx.Peoples.Add(new People { Id = Guid.NewGuid(), Name = "René Zellweger", PersonType = "Actor" });
            ctx.Peoples.Add(new People { Id = Guid.NewGuid(), Name = "Jean Dujardin", PersonType = "Actor" });
            ctx.SaveChanges();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        _repository = new PeopleRepository(factory.Object, new ItemTypeLookup());
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Theory]
    [InlineData("René")] // exact accented term — this failed before the fix
    [InlineData("Ren")] // accented name matched without the accent already
    [InlineData("rené")] // case-insensitive accented term
    [InlineData("ren")]
    public void GetPeople_NameContains_MatchesAccentedName(string searchTerm)
    {
        var result = _repository.GetPeople(new InternalPeopleQuery
        {
            NameContains = searchTerm
        });

        var item = Assert.Single(result.Items);
        Assert.Equal("René Zellweger", item.Name);
    }

    [Fact]
    public void GetPeople_NameContains_DoesNotReturnUnrelatedPeople()
    {
        var result = _repository.GetPeople(new InternalPeopleQuery
        {
            NameContains = "René"
        });

        Assert.All(result.Items, p => Assert.NotEqual("Jean Dujardin", p.Name));
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
