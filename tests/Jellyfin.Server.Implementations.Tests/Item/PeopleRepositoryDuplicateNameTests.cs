using System;
using Emby.Server.Implementations.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// People whose names differ only in case collapse into the row with the smallest id. The ids are picked
/// so that their canonical order is the opposite of both their <see cref="Guid.ToByteArray()"/> byte order
/// and the uniqueidentifier order of SQL Server.
/// </summary>
public sealed class PeopleRepositoryDuplicateNameTests : JellyfinDbTestFixture
{
    private readonly PeopleRepository _repository;

    public PeopleRepositoryDuplicateNameTests()
    {
        using (var context = CreateDbContext())
        {
            context.Peoples.Add(new People { Id = Guid.Parse("10000000-0000-0000-0000-000000000000"), Name = "ANA", PersonType = "Director" });
            context.Peoples.Add(new People { Id = Guid.Parse("0fffffff-0000-0000-0000-0000000000ff"), Name = "Ana", PersonType = "Actor" });
            context.SaveChanges();
        }

        _repository = new PeopleRepository(CreateDbContextFactory(), new ItemTypeLookup(), new Mock<IItemQueryHelpers>().Object);
    }

    [Fact]
    public void GetPeople_NamesDifferingInCase_ReturnsRowWithLowestId()
    {
        var result = _repository.GetPeople(new InternalPeopleQuery());

        Assert.Equal("Ana", Assert.Single(result.Items).Name);
        Assert.Equal(1, result.TotalRecordCount);
    }
}
