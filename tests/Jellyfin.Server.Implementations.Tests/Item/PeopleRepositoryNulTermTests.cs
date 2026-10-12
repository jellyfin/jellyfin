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
/// A NUL character in the text a caller filters people by is ignored, so the filter returns what it returns
/// without the NUL. Some engines reject a NUL in a query parameter.
/// </summary>
public sealed class PeopleRepositoryNulTermTests : SqliteDbTestFixture
{
    private static readonly string[] _allNames = ["ALICE", "Bob", "Rob", "Zoe", "bo", "bobby"];

    private readonly PeopleRepository _repository;

    public PeopleRepositoryNulTermTests()
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
    [InlineData("bob")]
    [InlineData("bo\0b")]
    [InlineData("bob\0")]
    [InlineData("\0bob")]
    public void GetPeople_NameStartsWithContainingNul_IgnoresTheNul(string prefix)
    {
        Assert.Equal(["Bob", "bobby"], Names(new InternalPeopleQuery { NameStartsWith = prefix }));
    }

    [Theory]
    [InlineData("ob")]
    [InlineData("o\0b")]
    [InlineData("ob\0")]
    [InlineData("\0ob")]
    public void GetPeople_NameContainsContainingNul_IgnoresTheNul(string fragment)
    {
        Assert.Equal(["Bob", "Rob", "bobby"], Names(new InternalPeopleQuery { NameContains = fragment }));
    }

    [Theory]
    [InlineData("bob")]
    [InlineData("bo\0b")]
    [InlineData("bob\0")]
    [InlineData("\0bob")]
    public void GetPeople_NameLessThanContainingNul_IgnoresTheNul(string bound)
    {
        Assert.Equal(["ALICE", "bo"], Names(new InternalPeopleQuery { NameLessThan = bound }));
    }

    [Theory]
    [InlineData("bob")]
    [InlineData("bo\0b")]
    [InlineData("bob\0")]
    [InlineData("\0bob")]
    public void GetPeople_NameStartsWithOrGreaterContainingNul_IgnoresTheNul(string bound)
    {
        Assert.Equal(["Bob", "Rob", "Zoe", "bobby"], Names(new InternalPeopleQuery { NameStartsWithOrGreater = bound }));
    }

    [Fact]
    public void GetPeople_NameFiltersWithOnlyNul_AreIgnored()
    {
        Assert.Equal(_allNames, Names(new InternalPeopleQuery { NameStartsWith = "\0" }));
        Assert.Equal(_allNames, Names(new InternalPeopleQuery { NameContains = "\0" }));
        Assert.Equal(_allNames, Names(new InternalPeopleQuery { NameLessThan = "\0" }));
        Assert.Equal(_allNames, Names(new InternalPeopleQuery { NameStartsWithOrGreater = "\0" }));
    }

    private string[] Names(InternalPeopleQuery query)
        => _repository.GetPeople(query).Items.Select(p => p.Name).Order(StringComparer.Ordinal).ToArray();
}
