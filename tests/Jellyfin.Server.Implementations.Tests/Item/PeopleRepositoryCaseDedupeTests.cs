using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The person list collapses rows whose names differ only in case, comparing the lower case names the rows hold.
/// They are lower-cased in code, so names that differ only in the case of a non-ASCII letter collapse as well.
/// </summary>
public sealed class PeopleRepositoryCaseDedupeTests : JellyfinDbTestFixture
{
    private readonly PeopleRepository _repository;

    public PeopleRepositoryCaseDedupeTests()
    {
        using (var context = CreateDbContext())
        {
            context.Peoples.Add(new People { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Bob", NameLower = MatchNormalizer.NameLower("Bob"), PersonType = "Actor" });
            context.Peoples.Add(new People { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "bob", NameLower = MatchNormalizer.NameLower("bob"), PersonType = "Director" });
            context.Peoples.Add(new People { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "Émile", NameLower = MatchNormalizer.NameLower("Émile"), PersonType = "Actor" });
            context.Peoples.Add(new People { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = "émile", NameLower = MatchNormalizer.NameLower("émile"), PersonType = "Director" });
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
    public void GetPeople_NonAsciiNamesDifferingInCase_CollapseIntoOneKeepingTheLowestId()
    {
        var result = _repository.GetPeople(new InternalPeopleQuery());

        Assert.Equal(["Bob", "Émile"], result.Items.Select(p => p.Name).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(2, result.TotalRecordCount);
    }
}
