using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;
using ServerLibraryManager = Emby.Server.Implementations.Library.LibraryManager;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// Covers which person a credit is linked to when it reaches <see cref="ServerLibraryManager.UpdatePeople"/>
/// without the provider ids that linked it, as the metadata editor and an NFO hand it in.
/// </summary>
[Collection("LibraryManagerTests")]
public sealed class PersonCreditResolutionTests : SqliteDbTestFixture
{
    private static readonly Guid _movieId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid _linkedPersonId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid _otherPersonId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IFileSystem? _previousFileSystem;
    private readonly string _peoplePath;
    private readonly ServerLibraryManager _libraryManager;

    public PersonCreditResolutionTests()
    {
        // Person.GetPath resolves through these process-wide statics; restored in Dispose.
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousFileSystem = BaseItem.FileSystem;
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.GetValidFilename(It.IsAny<string>())).Returns<string>(name => name);
        BaseItem.FileSystem = fileSystem.Object;
        _peoplePath = Path.Combine(Path.GetTempPath(), "jellyfin-person-credit-" + Guid.NewGuid().ToString("N"));
        var configuration = new Mock<IServerConfigurationManager>();
        configuration.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        configuration.Setup(c => c.ApplicationPaths.PeoplePath).Returns(_peoplePath);
        BaseItem.ConfigurationManager = configuration.Object;

        var lookup = new ItemTypeLookup();
        using (var ctx = CreateDbContext())
        {
            ctx.BaseItems.AddRange(
                new BaseItemEntity
                {
                    Id = _movieId,
                    Type = lookup.BaseItemKindNames[BaseItemKind.Movie],
                    Name = "Movie",
                    CleanName = "movie",
                    MediaType = "Video",
                    IsMovie = true
                },
                CreatePersonEntity(lookup, _linkedPersonId, "John Smith"),
                CreatePersonEntity(lookup, _otherPersonId, "Someone Else"));
            ctx.BaseItemProviders.Add(new BaseItemProvider
            {
                Item = null!,
                ItemId = _linkedPersonId,
                ProviderId = "Tmdb",
                ProviderValue = "1"
            });

            // Linked by its Tmdb id to a person who carries a different spelling.
            var creditId = Guid.NewGuid();
            ctx.Peoples.Add(new People
            {
                Id = creditId,
                Name = "Jonathan Smith",
                CleanName = "Jonathan Smith".GetCleanValue(),
                ItemId = _linkedPersonId,
                PersonType = nameof(PersonKind.Actor)
            });
            ctx.PeopleBaseItemMap.Add(new PeopleBaseItemMap
            {
                Item = null!,
                ItemId = _movieId,
                People = null!,
                PeopleId = creditId,
                ListOrder = 0,
                Role = "Hero"
            });
            ctx.SaveChanges();
        }

        _libraryManager = CreateLibraryManager(lookup);
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        BaseItem.FileSystem = _previousFileSystem!;
        if (Directory.Exists(_peoplePath))
        {
            Directory.Delete(_peoplePath, true);
        }

        base.Dispose(disposing);
    }

    [Fact]
    public void UpdatePeople_CreditWithoutIds_KeepsTheItemsExistingLink()
    {
        // What an NFO the saver wrote hands back: name, role and type, no provider ids.
        _libraryManager.UpdatePeople(new Movie { Id = _movieId }, [Credit("Jonathan Smith")]);

        Assert.Equal(_linkedPersonId, GetLinkedPersonId());
    }

    [Fact]
    public void UpdatePeople_EditorSaveCarryingThePersonId_KeepsTheLink()
    {
        var credit = Credit("Jonathan Smith");
        credit.PersonItemId = _linkedPersonId;

        _libraryManager.UpdatePeople(new Movie { Id = _movieId }, [credit]);

        Assert.Equal(_linkedPersonId, GetLinkedPersonId());
    }

    [Fact]
    public void UpdatePeople_EditorRenamedTheCastEntry_DropsTheStalePersonId()
    {
        // The editor keeps the row's id when its name is changed to somebody else.
        var credit = Credit("Someone Else");
        credit.PersonItemId = _linkedPersonId;

        _libraryManager.UpdatePeople(new Movie { Id = _movieId }, [credit]);

        Assert.Equal(_otherPersonId, GetLinkedPersonId());
    }

    [Fact]
    public void UpdatePeople_CreditWithProviderId_ResolvesThroughIt()
    {
        var credit = Credit("Jonathan Smith");
        credit.ProviderIds["Tmdb"] = "1";

        _libraryManager.UpdatePeople(new Movie { Id = _movieId }, [credit]);

        Assert.Equal(_linkedPersonId, GetLinkedPersonId());
    }

    private static PersonInfo Credit(string name) => new()
    {
        Name = name,
        Role = "Hero",
        Type = PersonKind.Actor
    };

    private static BaseItemEntity CreatePersonEntity(ItemTypeLookup lookup, Guid id, string name) => new()
    {
        Id = id,
        Type = lookup.BaseItemKindNames[BaseItemKind.Person],
        Name = name,
        CleanName = name.GetCleanValue(),
        DateCreated = DateTime.UtcNow
    };

    private Guid GetLinkedPersonId()
    {
        using var ctx = CreateDbContext();
        return ctx.PeopleBaseItemMap
            .Include(e => e.People)
            .Where(e => e.ItemId.Equals(_movieId))
            .Select(e => e.People.ItemId)
            .Single();
    }

    private ServerLibraryManager CreateLibraryManager(ItemTypeLookup lookup)
    {
        var repository = CreateBaseItemRepository(lookup);
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        var configuration = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configuration.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        configuration.Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        fixture.Inject<IItemRepository>(repository);
        fixture.Inject<IPeopleRepository>(new PeopleRepository(CreateDbContextFactory(), lookup, new Mock<IItemQueryHelpers>().Object));
        fixture.Inject<ILinkedChildrenService>(new LinkedChildrenService(CreateDbContextFactory(), lookup, repository));
        return fixture.Create<ServerLibraryManager>();
    }
}
