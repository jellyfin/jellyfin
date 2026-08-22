using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Locking;
using Jellyfin.Database.Providers.Sqlite;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Querying;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers the link between a credit and the person item it belongs to.
/// </summary>
/// <remarks>
/// The link is the person item id, not the name, so a renamed person keeps everything it is
/// credited on.
/// </remarks>
public sealed class PersonCreditLinkTests : IDisposable
{
    private static readonly Guid _movieId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _personItemId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid _creditId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<JellyfinDbContext> _dbOptions;
    private readonly BaseItemRepository _repository;

    public PersonCreditLinkTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<JellyfinDbContext>()
            .UseSqlite(_connection)
            .Options;

        var itemTypeLookup = new ItemTypeLookup();

        using (var ctx = CreateDbContext())
        {
            ctx.Database.EnsureCreated();
            ctx.BaseItems.Add(new BaseItemEntity
            {
                Id = _movieId,
                Type = itemTypeLookup.BaseItemKindNames[BaseItemKind.Movie],
                Name = "Movie",
                CleanName = "movie",
                MediaType = "Video",
                IsMovie = true,
                IsFolder = false,
                IsVirtualItem = false
            });
            ctx.BaseItems.Add(new BaseItemEntity
            {
                Id = _personItemId,
                Type = itemTypeLookup.BaseItemKindNames[BaseItemKind.Person],
                Name = "Zoe Saldana",
                CleanName = "zoe saldana",
                IsFolder = false,
                IsVirtualItem = false
            });
            ctx.Peoples.Add(new People
            {
                Id = _creditId,
                Name = "Zoe Saldana",
                CleanName = "zoe saldana",
                ItemId = _personItemId,
                PersonType = "Actor"
            });
            ctx.PeopleBaseItemMap.Add(new PeopleBaseItemMap
            {
                Item = null!,
                ItemId = _movieId,
                People = null!,
                PeopleId = _creditId,
                ListOrder = 0,
                Role = "Hero"
            });
            ctx.SaveChanges();
        }

        var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
        factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);

        var serverConfigurationManager = new Mock<IServerConfigurationManager>();
        serverConfigurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

        _repository = new BaseItemRepository(
            factory.Object,
            new Mock<IServerApplicationHost>().Object,
            itemTypeLookup,
            serverConfigurationManager.Object,
            NullLogger<BaseItemRepository>.Instance);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public void GetItemList_ByPersonId_ReturnsWhatThePersonIsCreditedOn()
    {
        var result = _repository.GetItemList(QueryByPerson());

        var item = Assert.Single(result);
        Assert.Equal(_movieId, item.Id);
    }

    [Theory]
    // A user correcting the spelling, a provider returning a translation, and a provider returning
    // another person entirely all reach the database the same way: the person item's name changes.
    [InlineData("Zoe Saldaña")]
    [InlineData("ゾーイ・サルダナ")]
    [InlineData("Someone Else")]
    public void GetItemList_ByPersonId_AfterTheItemWasRenamed_StillReturnsIt(string newName)
    {
        Rename(newName);

        var result = _repository.GetItemList(QueryByPerson());

        var item = Assert.Single(result);
        Assert.Equal(_movieId, item.Id);
    }

    [Fact]
    public void GetItemList_ByPersonId_MediaCreditingTheNewSpelling_JoinsTheSameFilmography()
    {
        // The new spelling gets its own row, but resolves to the same person item, so both credits
        // have to come back as one filmography.
        Rename("Zoe Saldana Perego");
        var secondMovieId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        AddCreditUnderANewSpelling(secondMovieId, "Zoe Saldana Perego");

        var result = _repository.GetItemList(QueryByPerson());

        Assert.Equal(
            [_movieId, secondMovieId],
            result.Select(e => e.Id).OrderBy(e => e.ToString(), StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void GetItemList_ByName_ResolvesAPersonByTheNameItCarriesNow()
    {
        // The query GetPerson falls back on when the name hash misses. A credit under the new name
        // resolves because the rename rewrote the item's clean name, not because the spellings are
        // matched to each other; the old name reaches the person by its folder hash instead.
        Rename("Zoe Saldana Repaggio");

        Assert.Single(_repository.GetItemList(QueryByName("Zoe Saldana Repaggio")));
        Assert.Empty(_repository.GetItemList(QueryByName("Zoe Saldana")));
    }

    [Fact]
    public void GetItemList_ByProviderId_FindsThePersonWhateverItIsCalledNow()
    {
        // The lookup a credit is resolved with before its name is considered. Renaming the person does
        // not touch it, which is the point: a provider's id identifies the human, the spelling does not.
        using (var ctx = CreateDbContext())
        {
            ctx.BaseItemProviders.Add(new BaseItemProvider
            {
                Item = null!,
                ItemId = _personItemId,
                ProviderId = "Tmdb",
                ProviderValue = "1234"
            });
            ctx.SaveChanges();
        }

        Rename("Someone Else");

        var result = _repository.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Person],
            HasAnyProviderId = new Dictionary<string, string> { ["Tmdb"] = "1234" },
            OrderBy = [(ItemSortBy.DateCreated, SortOrder.Ascending)],
            Limit = 1,
            DtoOptions = new DtoOptions(true)
        });

        var item = Assert.Single(result);
        Assert.Equal(_personItemId, item.Id);
    }

    [Fact]
    public void GetItemList_IsDeadPerson_DoesNotClaimARenamedPerson()
    {
        // A dead person is one nothing credits any more. Keyed off the link, so renaming a person
        // is not what makes people validation delete it.
        Rename("Someone Else");

        var result = _repository.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Person],
            IsDeadPerson = true
        });

        Assert.Empty(result);
    }

    [Fact]
    public void GetItemList_IsDeadPerson_FindsAPersonNothingCredits()
    {
        using (var ctx = CreateDbContext())
        {
            ctx.PeopleBaseItemMap.RemoveRange(ctx.PeopleBaseItemMap);
            ctx.Peoples.RemoveRange(ctx.Peoples);
            ctx.SaveChanges();
        }

        var result = _repository.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Person],
            IsDeadPerson = true
        });

        var item = Assert.Single(result);
        Assert.Equal(_personItemId, item.Id);
    }

    private static InternalItemsQuery QueryByPerson()
    {
        return new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            PersonIds = [_personItemId]
        };
    }

    private static InternalItemsQuery QueryByName(string name)
    {
        return new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Person],
            Name = name
        };
    }

    // A film crediting the person under a spelling with no row of its own yet.
    private void AddCreditUnderANewSpelling(Guid movieId, string creditedName)
    {
        using var ctx = CreateDbContext();
        var creditId = Guid.NewGuid();
        ctx.BaseItems.Add(new BaseItemEntity
        {
            Id = movieId,
            Type = new ItemTypeLookup().BaseItemKindNames[BaseItemKind.Movie],
            Name = "Second Movie",
            CleanName = "second movie",
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        });
        ctx.Peoples.Add(new People
        {
            Id = creditId,
            Name = creditedName,
            CleanName = creditedName.GetCleanValue(),
            ItemId = _personItemId,
            PersonType = "Actor"
        });
        ctx.PeopleBaseItemMap.Add(new PeopleBaseItemMap
        {
            Item = null!,
            ItemId = movieId,
            People = null!,
            PeopleId = creditId,
            ListOrder = 0,
            Role = "Hero"
        });
        ctx.SaveChanges();
    }

    private void Rename(string newName)
    {
        using var ctx = CreateDbContext();
        var person = ctx.BaseItems.Single(e => e.Id.Equals(_personItemId));
        person.Name = newName;
        person.CleanName = newName.GetCleanValue();
        ctx.SaveChanges();
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
