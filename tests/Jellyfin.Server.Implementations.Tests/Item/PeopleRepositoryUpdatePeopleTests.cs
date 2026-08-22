using System;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class PeopleRepositoryUpdatePeopleTests : IDisposable
{
    private static readonly Guid _itemId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly PeopleRepository _repository;
    private readonly string _personTypeName;

    public PeopleRepositoryUpdatePeopleTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        var itemTypeLookup = new ItemTypeLookup();
        _personTypeName = itemTypeLookup.BaseItemKindNames[BaseItemKind.Person];

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();
            ctx.BaseItems.Add(new BaseItemEntity
            {
                Id = _itemId,
                Type = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
                Name = "Movie",
                MediaType = "Video",
                IsMovie = true,
                IsFolder = false,
                IsVirtualItem = false
            });
            ctx.SaveChanges();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        _repository = new PeopleRepository(
            factory.Object,
            itemTypeLookup,
            new Mock<IItemQueryHelpers>().Object);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void UpdatePeople_SamePersonAndTypeWithDifferentRoles_KeepsEveryCredit()
    {
        _repository.UpdatePeople(_itemId, [
            CreatePerson("Person A", PersonKind.Writer, "Novel"),
            CreatePerson("Person A", PersonKind.Writer, "Screenplay")
        ]);

        using var ctx = CreateDbContext();
        Assert.Single(ctx.Peoples);
        Assert.Equal(
            ["Novel", "Screenplay"],
            ctx.PeopleBaseItemMap.OrderBy(e => e.ListOrder).Select(e => e.Role ?? string.Empty).ToArray());
    }

    [Fact]
    public void UpdatePeople_CreditsDifferingOnlyInCase_AreDeduped()
    {
        _repository.UpdatePeople(_itemId, [
            CreatePerson("Person A", PersonKind.Actor, "Hero"),
            CreatePerson("person a", PersonKind.Actor, "hero")
        ]);

        using var ctx = CreateDbContext();
        Assert.Single(ctx.Peoples);
        var map = Assert.Single(ctx.PeopleBaseItemMap);
        Assert.Equal("Hero", map.Role);
    }

    [Theory]
    [InlineData("Zoe Saldaña", "Zoe Saldana")]
    [InlineData("Yûki Kaji", "Yuki Kaji")]
    [InlineData("A. J. Cook", "A.J. Cook")]
    [InlineData("Brian O'Neill", "Brian O’Neill")]
    [InlineData("Anne-Marie", "Anne Marie")]
    public void UpdatePeople_CreditsDifferingOnlyInSpelling_AreOnePerson(string first, string second)
    {
        _repository.UpdatePeople(_itemId, [
            CreatePerson(first, PersonKind.Actor, "Hero"),
            CreatePerson(second, PersonKind.Actor, "Hero")
        ]);

        using var ctx = CreateDbContext();
        Assert.Single(ctx.Peoples);
        Assert.Single(ctx.PeopleBaseItemMap);
    }

    [Fact]
    public void UpdatePeople_CreditForAnExistingPersonSpelledDifferently_ReusesThatPerson()
    {
        _repository.UpdatePeople(_itemId, [CreatePerson("Zoe Saldaña", PersonKind.Actor, "Hero")]);
        Guid personId;
        using (var before = CreateDbContext())
        {
            personId = before.Peoples.Single().Id;
        }

        _repository.UpdatePeople(_itemId, [CreatePerson("Zoe Saldana", PersonKind.Actor, "Hero")]);

        using var ctx = CreateDbContext();
        Assert.Equal(personId, ctx.Peoples.Single().Id);
        Assert.Equal(personId, ctx.PeopleBaseItemMap.Single().PeopleId);
    }

    [Fact]
    public void UpdatePeople_DifferentPeople_StayApart()
    {
        _repository.UpdatePeople(_itemId, [
            CreatePerson("Ken'ichi Ogata", PersonKind.Actor, "Hero"),
            CreatePerson("Kenichi Ogata", PersonKind.Actor, "Hero")
        ]);

        using var ctx = CreateDbContext();
        Assert.Equal(2, ctx.Peoples.Count());
    }

    [Fact]
    public void UpdatePeople_SamePersonAsDifferentTypes_CreatesOnePersonPerType()
    {
        _repository.UpdatePeople(_itemId, [
            CreatePerson("Person A", PersonKind.Actor, "Hero"),
            CreatePerson("Person A", PersonKind.Director, string.Empty)
        ]);

        using var ctx = CreateDbContext();
        Assert.Equal(2, ctx.Peoples.Count());
        Assert.Equal(2, ctx.PeopleBaseItemMap.Count());
    }

    [Fact]
    public void UpdatePeople_RepeatedUpdate_ReusesMappingsAndRefreshesOrder()
    {
        _repository.UpdatePeople(_itemId, [
            CreatePerson("Person A", PersonKind.Actor, "Hero"),
            CreatePerson("Person B", PersonKind.Actor, "Sidekick")
        ]);

        Guid[] peopleIdsBefore;
        using (var ctx = CreateDbContext())
        {
            peopleIdsBefore = ctx.Peoples.Select(e => e.Id).OrderBy(e => e).ToArray();
        }

        // Reversed order, so the list order of both mappings has to be rewritten.
        _repository.UpdatePeople(_itemId, [
            CreatePerson("Person B", PersonKind.Actor, "Sidekick"),
            CreatePerson("Person A", PersonKind.Actor, "Hero")
        ]);

        using var after = CreateDbContext();
        Assert.Equal(peopleIdsBefore, after.Peoples.Select(e => e.Id).OrderBy(e => e).ToArray());
        Assert.Equal(
            ["Sidekick", "Hero"],
            after.PeopleBaseItemMap.OrderBy(e => e.ListOrder).Select(e => e.Role ?? string.Empty).ToArray());
    }

    [Fact]
    public void UpdatePeople_CreditRemoved_DropsOnlyThatMapping()
    {
        _repository.UpdatePeople(_itemId, [
            CreatePerson("Person A", PersonKind.Writer, "Novel"),
            CreatePerson("Person A", PersonKind.Writer, "Screenplay")
        ]);

        _repository.UpdatePeople(_itemId, [
            CreatePerson("Person A", PersonKind.Writer, "Novel")
        ]);

        using var ctx = CreateDbContext();
        var map = Assert.Single(ctx.PeopleBaseItemMap);
        Assert.Equal("Novel", map.Role);
    }

    [Fact]
    public void UpdatePeople_RoleCaseChanged_KeepsExistingMapping()
    {
        _repository.UpdatePeople(_itemId, [CreatePerson("Person A", PersonKind.Actor, "Hero")]);

        _repository.UpdatePeople(_itemId, [CreatePerson("Person A", PersonKind.Actor, "HERO")]);

        using var ctx = CreateDbContext();
        var map = Assert.Single(ctx.PeopleBaseItemMap);
        Assert.Equal("Hero", map.Role);
    }

    [Fact]
    public void UpdatePeople_CreditWithAResolvedPerson_StoresTheLink()
    {
        var personItemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var person = CreatePerson("Person A", PersonKind.Actor, "Hero");
        person.PersonItemId = personItemId;

        _repository.UpdatePeople(_itemId, [person]);

        using var ctx = CreateDbContext();
        Assert.Equal(personItemId, ctx.Peoples.Single().ItemId);
    }

    [Fact]
    public void UpdatePeople_ExistingCreditWithoutALink_AdoptsTheResolvedPerson()
    {
        // Written before the person item existed, so it has nothing to point at until a scan resolves one.
        _repository.UpdatePeople(_itemId, [CreatePerson("Person A", PersonKind.Actor, "Hero")]);

        var personItemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var person = CreatePerson("Person A", PersonKind.Actor, "Hero");
        person.PersonItemId = personItemId;
        _repository.UpdatePeople(_itemId, [person]);

        using var ctx = CreateDbContext();
        Assert.Equal(personItemId, ctx.Peoples.Single().ItemId);
    }

    [Fact]
    public void UpdatePeople_CreditForAnotherPersonOfTheSameName_GetsItsOwnRow()
    {
        var personItemId = AddPersonItem("Person A");
        var person = CreatePerson("Person A", PersonKind.Actor, "Hero");
        person.PersonItemId = personItemId;
        _repository.UpdatePeople(_itemId, [person]);

        // Another human of the same name: never repointed, so the second one owns a row.
        var other = CreatePerson("Person A", PersonKind.Actor, "Hero");
        var otherItemId = AddPersonItem("Person A Elsewhere");
        other.PersonItemId = otherItemId;
        _repository.UpdatePeople(_itemId, [other]);

        using var ctx = CreateDbContext();
        var itemIds = ctx.Peoples.Select(e => e.ItemId).ToArray();
        Assert.Equal(2, itemIds.Length);
        Assert.Contains(personItemId, itemIds);
        Assert.Contains(otherItemId, itemIds);
    }

    [Fact]
    public void UpdatePeople_TwoPeopleOfOneNameOnTheSameItem_AreBothCredited()
    {
        var first = CreatePerson("Person A", PersonKind.Actor, "Hero");
        first.PersonItemId = AddPersonItem("Person A");
        var second = CreatePerson("Person A", PersonKind.Actor, "Villain");
        second.PersonItemId = AddPersonItem("Person A Elsewhere");

        _repository.UpdatePeople(_itemId, [first, second]);

        using var ctx = CreateDbContext();
        Assert.Equal(2, ctx.Peoples.Count());
        Assert.Equal(
            ["Hero", "Villain"],
            ctx.PeopleBaseItemMap.Select(e => e.Role ?? string.Empty).OrderBy(e => e).ToArray());
    }

    [Fact]
    public void LinkCreditsToItem_FillsOnlyTheCreditsThatHaveNoItem()
    {
        _repository.UpdatePeople(_itemId, [CreatePerson("Person A", PersonKind.Actor, "Hero")]);
        var linked = CreatePerson("Person B", PersonKind.Actor, "Sidekick");
        linked.PersonItemId = AddPersonItem("Person B");
        _repository.UpdatePeople(_itemId, [CreatePerson("Person A", PersonKind.Actor, "Hero"), linked]);

        Assert.Equal(["Person A"], _repository.GetUnlinkedCredits().Select(e => e.Name));

        var personItemId = AddPersonItem("Person A");
        Assert.Equal(1, _repository.LinkCreditsToItem("Person A", PersonKind.Actor, personItemId));
        Assert.Empty(_repository.GetUnlinkedCredits());

        using var ctx = CreateDbContext();
        Assert.Equal(personItemId, ctx.Peoples.Single(e => e.Name == "Person A").ItemId);
        Assert.Equal(linked.PersonItemId, ctx.Peoples.Single(e => e.Name == "Person B").ItemId);
    }

    [Fact]
    public void LinkCreditsToItem_CreditPointingAtAnItemThatIsGone_IsReResolved()
    {
        // A link that no longer resolves has nothing to protect.
        var stale = CreatePerson("Person A", PersonKind.Actor, "Hero");
        stale.PersonItemId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        _repository.UpdatePeople(_itemId, [stale]);

        Assert.Equal(["Person A"], _repository.GetUnlinkedCredits().Select(e => e.Name));

        var personItemId = AddPersonItem("Person A");
        Assert.Equal(1, _repository.LinkCreditsToItem("Person A", PersonKind.Actor, personItemId));
        Assert.Empty(_repository.GetUnlinkedCredits());

        using var ctx = CreateDbContext();
        Assert.Equal(personItemId, ctx.Peoples.Single().ItemId);
    }

    [Fact]
    public void GetPeople_PersonCreditedUnderTwoSpellings_IsListedOnce()
    {
        // After a rename the person holds a row per spelling; /Persons lists people, not spellings.
        var personItemId = AddPersonItem("Zoe Saldana");

        var oldSpelling = CreatePerson("Zoe Saldana", PersonKind.Actor, "Hero");
        oldSpelling.PersonItemId = personItemId;
        _repository.UpdatePeople(_itemId, [oldSpelling]);

        var newSpelling = CreatePerson("Zoe Saldana Perego", PersonKind.Actor, "Hero");
        newSpelling.PersonItemId = personItemId;
        _repository.UpdatePeople(_itemId, [oldSpelling, newSpelling]);

        using (var ctx = CreateDbContext())
        {
            Assert.Equal(2, ctx.Peoples.Count());
        }

        var result = _repository.GetPeople(new InternalPeopleQuery { EnableTotalRecordCount = true });

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalRecordCount);
    }

    [Fact]
    public void LinkCreditsToItem_SameNameDifferentKinds_LinksOnlyTheMatchingKind()
    {
        // The Artist resolves to a MusicArtist and the Composer to a Person.
        _repository.UpdatePeople(_itemId, [
            CreatePerson("Miles Davis", PersonKind.Artist, string.Empty),
            CreatePerson("Miles Davis", PersonKind.Composer, string.Empty)
        ]);

        var artistItemId = AddPersonItem("Miles Davis");
        Assert.Equal(1, _repository.LinkCreditsToItem("Miles Davis", PersonKind.Artist, artistItemId));

        using var ctx = CreateDbContext();
        Assert.Equal(artistItemId, ctx.Peoples.Single(e => e.PersonType == nameof(PersonKind.Artist)).ItemId);
        Assert.Equal(Guid.Empty, ctx.Peoples.Single(e => e.PersonType == nameof(PersonKind.Composer)).ItemId);
    }

    [Fact]
    public void GetUnlinkedCredits_ReturnsTheKindWithTheName()
    {
        _repository.UpdatePeople(_itemId, [
            CreatePerson("Miles Davis", PersonKind.Artist, string.Empty),
            CreatePerson("Miles Davis", PersonKind.Composer, string.Empty)
        ]);

        var unlinked = _repository.GetUnlinkedCredits();

        Assert.Equal(
            [PersonKind.Artist, PersonKind.Composer],
            unlinked.Select(e => e.Type).OrderBy(e => e.ToString(), StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void LinkCreditsToItem_MatchesOnTheCleanName()
    {
        _repository.UpdatePeople(_itemId, [CreatePerson("Zoe Saldaña", PersonKind.Actor, "Hero")]);

        var personItemId = AddPersonItem("Zoe Saldana");
        Assert.Equal(1, _repository.LinkCreditsToItem("Zoe Saldana", PersonKind.Actor, personItemId));

        using var ctx = CreateDbContext();
        Assert.Equal(personItemId, ctx.Peoples.Single().ItemId);
    }

    private static PersonInfo CreatePerson(string name, PersonKind type, string role)
    {
        return new PersonInfo
        {
            Name = name,
            Type = type,
            Role = role
        };
    }

    // A link only counts as live while the item it points at exists.
    private Guid AddPersonItem(string name)
    {
        var id = Guid.NewGuid();
        using var ctx = CreateDbContext();
        ctx.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = _personTypeName,
            Name = name,
            CleanName = name.ToLowerInvariant(),
            IsFolder = false,
            IsVirtualItem = false
        });
        ctx.SaveChanges();

        return id;
    }

    private JellyfinDbContext CreateDbContext()
    {
        return new JellyfinDbContext(
            _dbOptions,
            NullLogger<JellyfinDbContext>.Instance,
            new SqliteDatabaseProvider(null!, NullLogger<SqliteDatabaseProvider>.Instance),
            new NoLockBehavior(NullLogger<NoLockBehavior>.Instance));
    }
}
