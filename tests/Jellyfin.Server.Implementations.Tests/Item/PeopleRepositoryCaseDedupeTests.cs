using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The person list collapses rows whose names differ only in case, comparing the names lower-cased in SQL.
/// SQL <c>lower()</c> folds ASCII only, so names that differ only in the case of a non-ASCII letter stay
/// separate.
/// </summary>
public sealed class PeopleRepositoryCaseDedupeTests : SqliteDbTestFixture
{
    private readonly PeopleRepository _repository;

    public PeopleRepositoryCaseDedupeTests()
    {
        using (var context = CreateDbContext())
        {
            context.Peoples.Add(new People { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Bob", PersonType = "Actor" });
            context.Peoples.Add(new People { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "bob", PersonType = "Director" });
            context.Peoples.Add(new People { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Émile", PersonType = "Actor" });
            context.Peoples.Add(new People { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = "émile", PersonType = "Director" });
            context.SaveChanges();
        }

        _repository = new PeopleRepository(CreateDbContextFactory(), new ItemTypeLookup(), new Mock<IItemQueryHelpers>().Object);
    }

    [Fact]
    public void GetPeople_AsciiNamesDifferingInCase_CollapseIntoOne()
    {
        var result = _repository.GetPeople(new InternalPeopleQuery());

        Assert.Equal("Bob", Assert.Single(result.Items, p => string.Equals(p.Name, "bob", StringComparison.OrdinalIgnoreCase)).Name);
    }

    [Fact]
    public void GetPeople_NonAsciiNamesDifferingInCase_StaySeparate_CurrentBehaviour()
    {
        var result = _repository.GetPeople(new InternalPeopleQuery());

        Assert.Equal(["Bob", "Émile", "émile"], result.Items.Select(p => p.Name).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(3, result.TotalRecordCount);
    }
}
