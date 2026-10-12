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
/// People are listed by lower-cased name in Unicode code point order, so '_' comes before letters and
/// accented letters after 'z'. The name range filters compare the lower-cased name the same way.
/// </summary>
public sealed class PeopleRepositoryNameOrderTests : SqliteDbTestFixture
{
    private readonly PeopleRepository _repository;

    public PeopleRepositoryNameOrderTests()
    {
        using (var context = CreateDbContext())
        {
            foreach (var name in new[] { "Bob", "Zed", "élan", "_zed", "alice", "Ángel" })
            {
                context.Peoples.Add(new People { Id = Guid.NewGuid(), Name = name, PersonType = "Actor" });
            }

            context.SaveChanges();
        }

        _repository = new PeopleRepository(CreateDbContextFactory(), new ItemTypeLookup(), new Mock<IItemQueryHelpers>().Object);
    }

    [Fact]
    public void GetPeople_NoFilter_OrdersByLowerCasedNameCodePoint()
    {
        var result = _repository.GetPeople(new InternalPeopleQuery());

        Assert.Equal(["_zed", "alice", "Bob", "Zed", "Ángel", "élan"], result.Items.Select(p => p.Name));
    }

    [Theory]
    [InlineData("a", null, new[] { "alice", "Bob", "Zed", "Ángel", "élan" })]
    [InlineData("é", null, new[] { "élan" })]
    [InlineData(null, "é", new[] { "_zed", "alice", "Bob", "Zed", "Ángel" })]
    public void GetPeople_NameRange_BoundsByLowerCasedNameCodePoint(string? nameStartsWithOrGreater, string? nameLessThan, string[] expected)
    {
        var result = _repository.GetPeople(new InternalPeopleQuery
        {
            NameStartsWithOrGreater = nameStartsWithOrGreater,
            NameLessThan = nameLessThan
        });

        Assert.Equal(expected, result.Items.Select(p => p.Name));
        Assert.Equal(expected.Length, result.TotalRecordCount);
    }
}
