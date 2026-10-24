using System;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers <see cref="InternalPeopleQuery.MustHaveItem"/>. A caller that hands back by-name items can
/// only return the people that have one, so the count it reports has to leave out the rest rather than
/// describe a larger set than it can page through.
/// </summary>
public sealed class PeopleRepositoryMustHaveItemTests : SqliteDbTestFixture
{
    private readonly PeopleRepository _people;
    private readonly Guid _movie = Guid.NewGuid();

    public PeopleRepositoryMustHaveItemTests()
    {
        var lookup = new ItemTypeLookup();
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = _movie,
                Name = "Movie",
                Type = lookup.BaseItemKindNames[BaseItemKind.Movie]
            });

            // Only one of the two credits has a by-name item behind it.
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = Guid.NewGuid(),
                Name = "With Item",
                Type = lookup.BaseItemKindNames[BaseItemKind.Person]
            });

            context.SaveChanges();
        }

        _people = new PeopleRepository(CreateDbContextFactory(), lookup, Mock.Of<IItemQueryHelpers>());
        _people.UpdatePeople(_movie, [
            new PersonInfo { Name = "With Item", Type = PersonKind.Actor },
            new PersonInfo { Name = "Without Item", Type = PersonKind.Actor }
        ]);
    }

    [Fact]
    public void WithoutMustHaveItem_ReturnsAndCountsEveryCredit()
    {
        var result = _people.GetPeople(new InternalPeopleQuery());

        Assert.Equal(2, result.TotalRecordCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public void MustHaveItem_DropsTheCreditWithoutAnItem()
    {
        var result = _people.GetPeople(new InternalPeopleQuery { MustHaveItem = true });

        Assert.Equal("With Item", Assert.Single(result.Items).Name);
    }

    [Fact]
    public void MustHaveItem_CountsOnlyWhatItCanReturn()
    {
        var result = _people.GetPeople(new InternalPeopleQuery { MustHaveItem = true });

        Assert.Equal(1, result.TotalRecordCount);
    }

    [Fact]
    public void MustHaveItem_CountAgreesWithThePageWhenLimited()
    {
        var result = _people.GetPeople(new InternalPeopleQuery { MustHaveItem = true, Limit = 10 });

        Assert.Equal(result.Items.Count, result.TotalRecordCount);
    }
}
