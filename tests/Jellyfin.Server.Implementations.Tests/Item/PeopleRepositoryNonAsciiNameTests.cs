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
/// The name filters of the person list compare the lower case name a row holds, which is lower-cased in code, so
/// they treat the case of a letter outside ASCII like the case of any other letter on every database.
/// </summary>
public sealed class PeopleRepositoryNonAsciiNameTests : JellyfinDbTestFixture
{
    private readonly PeopleRepository _repository;

    public PeopleRepositoryNonAsciiNameTests()
    {
        using (var context = CreateDbContext())
        {
            foreach (var name in new[] { "Émile Zola", "Zed", "ärni", "Éric", "Zoë" })
            {
                context.Peoples.Add(new People { Id = Guid.NewGuid(), Name = name, NameLower = MatchNormalizer.NameLower(name), PersonType = "Actor" });
            }

            context.SaveChanges();
        }

        _repository = new PeopleRepository(CreateDbContextFactory(), new ItemTypeLookup(), new Mock<IItemQueryHelpers>().Object);
    }

    [Theory]
    [InlineData("émile")]
    [InlineData("ÉMILE")]
    [InlineData("Émile")]
    public void GetPeople_NameStartsWithNonAscii_IgnoresCase(string prefix)
    {
        Assert.Equal(["Émile Zola"], Names(new InternalPeopleQuery { NameStartsWith = prefix }));
    }

    [Theory]
    [InlineData("mile z", "Émile Zola")]
    [InlineData("ÉMIL", "Émile Zola")]
    [InlineData("émil", "Émile Zola")]
    [InlineData("ZOLA", "Émile Zola")]
    [InlineData("ÄRN", "ärni")]
    [InlineData("ärn", "ärni")]
    [InlineData("ZOË", "Zoë")]
    [InlineData("zoë", "Zoë")]
    public void GetPeople_NameContainsNonAscii_IgnoresCase(string fragment, string expected)
    {
        Assert.Equal([expected], Names(new InternalPeopleQuery { NameContains = fragment }));
    }

    [Theory]
    [InlineData("é")]
    [InlineData("É")]
    public void GetPeople_NameLessThanNonAscii_ComparesTheLowerCaseName(string bound)
    {
        Assert.Equal(["Zed", "Zoë", "ärni"], Names(new InternalPeopleQuery { NameLessThan = bound }));
    }

    [Theory]
    [InlineData("é")]
    [InlineData("É")]
    public void GetPeople_NameStartsWithOrGreaterNonAscii_ComparesTheLowerCaseName(string bound)
    {
        Assert.Equal(["Émile Zola", "Éric"], Names(new InternalPeopleQuery { NameStartsWithOrGreater = bound }));
    }

    private string[] Names(InternalPeopleQuery query)
    {
        var result = _repository.GetPeople(query);
        Assert.Equal(result.Items.Count, result.TotalRecordCount);
        return result.Items.Select(p => p.Name).Order(StringComparer.Ordinal).ToArray();
    }
}
