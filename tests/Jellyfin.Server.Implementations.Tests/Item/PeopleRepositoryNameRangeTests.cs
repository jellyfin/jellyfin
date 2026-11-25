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
/// The letter picker on the person list filters with <c>NameStartsWithOrGreater</c> and <c>NameLessThan</c>.
/// Names keep the case a metadata provider wrote them in, so the range has to ignore case the same way
/// the list is sorted, or a capitalised name lands before every lowercase bound.
/// </summary>
public sealed class PeopleRepositoryNameRangeTests : SqliteDbTestFixture
{
    private readonly PeopleRepository _repository;

    public PeopleRepositoryNameRangeTests()
    {
        using (var context = CreateDbContext())
        {
            foreach (var name in new[] { "50 Cent", "alpha centauri", "Brad Pitt", "bob dylan", "Zoe Saldana" })
            {
                context.Peoples.Add(new People { Id = Guid.NewGuid(), Name = name, PersonType = "Actor" });
            }

            context.SaveChanges();
        }

        _repository = new PeopleRepository(CreateDbContextFactory(), new ItemTypeLookup(), new Mock<IItemQueryHelpers>().Object);
    }

    [Theory]
    [InlineData(null, "A", new[] { "50 Cent" })]
    [InlineData(null, "C", new[] { "50 Cent", "alpha centauri", "bob dylan", "Brad Pitt" })]
    [InlineData("B", "C", new[] { "bob dylan", "Brad Pitt" })]
    [InlineData("Y", null, new[] { "Zoe Saldana" })]
    public void GetPeople_NameRange_IgnoresCase(string? nameStartsWithOrGreater, string? nameLessThan, string[] expected)
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
