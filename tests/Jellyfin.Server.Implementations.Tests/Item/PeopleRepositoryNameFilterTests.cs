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
/// Person names keep the case a metadata provider wrote them in. The prefix, substring and range filters
/// ignore ASCII case on both sides of the comparison, whichever case the caller typed.
/// </summary>
public sealed class PeopleRepositoryNameFilterTests : JellyfinDbTestFixture
{
    private readonly PeopleRepository _repository;

    public PeopleRepositoryNameFilterTests()
    {
        using (var context = CreateDbContext())
        {
            foreach (var name in new[] { "Bob", "bobby", "Rob", "ALICE", "bo", "Zoe" })
            {
                context.Peoples.Add(new People { Id = Guid.NewGuid(), Name = name, PersonType = "Actor" });
            }

            context.SaveChanges();
        }

        _repository = new PeopleRepository(CreateDbContextFactory(), new ItemTypeLookup(), new Mock<IItemQueryHelpers>().Object);
    }

    [Theory]
    [InlineData("bo")]
    [InlineData("BO")]
    [InlineData("Bo")]
    public void GetPeople_NameStartsWith_IgnoresCase(string prefix)
    {
        var names = Names(new InternalPeopleQuery { NameStartsWith = prefix });

        Assert.Equal(["Bob", "bo", "bobby"], names);
    }

    [Theory]
    [InlineData("ob")]
    [InlineData("OB")]
    [InlineData("Ob")]
    public void GetPeople_NameContains_IgnoresCase(string fragment)
    {
        var names = Names(new InternalPeopleQuery { NameContains = fragment });

        Assert.Equal(["Bob", "Rob", "bobby"], names);
    }

    [Theory]
    [InlineData("Bob")]
    [InlineData("bob")]
    [InlineData("BOB")]
    public void GetPeople_NameLessThan_ComparesLowerCasedNames(string bound)
    {
        var names = Names(new InternalPeopleQuery { NameLessThan = bound });

        Assert.Equal(["ALICE", "bo"], names);
    }

    [Theory]
    [InlineData("Bob")]
    [InlineData("bob")]
    [InlineData("BOB")]
    public void GetPeople_NameStartsWithOrGreater_ComparesLowerCasedNames(string bound)
    {
        var names = Names(new InternalPeopleQuery { NameStartsWithOrGreater = bound });

        Assert.Equal(["Bob", "Rob", "Zoe", "bobby"], names);
    }

    private string[] Names(InternalPeopleQuery query)
        => _repository.GetPeople(query).Items.Select(p => p.Name).Order(StringComparer.Ordinal).ToArray();
}
