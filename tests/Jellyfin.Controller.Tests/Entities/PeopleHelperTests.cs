using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Controller.Tests.Entities;

public class PeopleHelperTests
{
    [Fact]
    public void AddPerson_SameNameSpelledDifferently_IsOneCredit()
    {
        var people = new List<PersonInfo>();

        PeopleHelper.AddPerson(people, new PersonInfo { Name = "Zoe Saldaña", Type = PersonKind.Actor });
        PeopleHelper.AddPerson(people, new PersonInfo { Name = "Zoe Saldana", Type = PersonKind.Actor, Role = "Neytiri" });

        var credit = Assert.Single(people);
        Assert.Equal("Neytiri", credit.Role);
    }

    [Fact]
    public void AddPerson_SameNameWithADifferentProviderId_AreTwoCredits()
    {
        var people = new List<PersonInfo>();

        PeopleHelper.AddPerson(people, WithTmdbId("John Smith", "1"));
        PeopleHelper.AddPerson(people, WithTmdbId("John Smith", "2"));

        Assert.Equal(2, people.Count);
    }

    [Fact]
    public void AddPerson_SameNameAndProviderId_IsOneCredit()
    {
        var people = new List<PersonInfo>();

        PeopleHelper.AddPerson(people, WithTmdbId("John Smith", "1"));
        PeopleHelper.AddPerson(people, WithTmdbId("John Smith", "1"));

        Assert.Single(people);
    }

    [Fact]
    public void AddPerson_OneCreditWithoutAnId_IsOneCredit()
    {
        var people = new List<PersonInfo>();

        PeopleHelper.AddPerson(people, new PersonInfo { Name = "John Smith", Type = PersonKind.Actor });
        PeopleHelper.AddPerson(people, WithTmdbId("John Smith", "1"));

        Assert.Single(people);
    }

    [Theory]
    [InlineData("/people/j/John Smith", "Person-John Smith")]
    [InlineData("/people/j/John Smith [Tmdb-2]", "Person-John Smith [Tmdb-2]")]
    public void GetUserDataKeys_TellTwoPeopleOfOneNameApart(string path, string expected)
    {
        // Listings collapse rows sharing the presentation key, and a deletion parks user data under it.
        var person = new Person { Name = "John Smith", Path = path };

        Assert.Equal(expected, person.GetUserDataKeys()[0]);
        Assert.Equal(expected, person.CreatePresentationUniqueKey());
    }

    private static PersonInfo WithTmdbId(string name, string id)
    {
        var person = new PersonInfo { Name = name, Type = PersonKind.Actor };
        person.SetProviderId(MetadataProvider.Tmdb, id);

        return person;
    }
}
