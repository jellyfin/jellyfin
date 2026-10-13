using System;
using System.Linq;
using System.Threading;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Testing;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using BaseItemKind = Jellyfin.Data.Enums.BaseItemKind;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The columns that hold the normalized original title of an item and the lower case name of a person are written by
/// the code that stores the item or the person, on insert and on update, so that the queries can use them without the
/// database folding case.
/// </summary>
public sealed class NormalisedMatchColumnWriteTests : JellyfinDbTestFixture
{
    private readonly ItemPersistenceService _service;
    private readonly PeopleRepository _people;
    private readonly ILibraryManager? _previousLibraryManager;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IRecordingsManager? _previousRecordingsManager;

    public NormalisedMatchColumnWriteTests()
    {
        _previousLibraryManager = BaseItem.LibraryManager;
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousRecordingsManager = MediaBrowser.Controller.Entities.Video.RecordingsManager;

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager.Setup(l => l.GetCollectionFolders(It.IsAny<BaseItem>())).Returns([]);
        BaseItem.LibraryManager = libraryManager.Object;

        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        BaseItem.ConfigurationManager = configurationManager.Object;

        MediaBrowser.Controller.Entities.Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        var appHost = new Mock<IServerApplicationHost>();
        appHost.Setup(h => h.ReverseVirtualPath(It.IsAny<string>())).Returns((string p) => p);
        appHost.Setup(h => h.ExpandVirtualPath(It.IsAny<string>())).Returns((string p) => p);

        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            DatabaseProvider,
            appHost.Object,
            NullLogger<ItemPersistenceService>.Instance);
        _people = new PeopleRepository(CreateDbContextFactory(), new ItemTypeLookup(), new Mock<IItemQueryHelpers>().Object);
    }

    protected override void Dispose(bool disposing)
    {
        BaseItem.LibraryManager = _previousLibraryManager!;
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        MediaBrowser.Controller.Entities.Video.RecordingsManager = _previousRecordingsManager!;
        base.Dispose(disposing);
    }

    [Theory]
    [InlineData("Émile Zola", "emile zola")]
    [InlineData("AC\\DC", "ac dc")]
    [InlineData("---", "")]
    [InlineData("", "")]
    public void SaveItems_NewItem_StoresTheNormalizedOriginalTitle(string originalTitle, string expected)
    {
        var movie = new Movie { Id = Guid.NewGuid(), Name = "Movie", OriginalTitle = originalTitle };

        _service.SaveItems([movie], CancellationToken.None);

        Assert.Equal(expected, ReadCleanOriginalTitle(movie.Id));
    }

    [Fact]
    public void SaveItems_NewItemWithoutOriginalTitle_StoresNull()
    {
        var movie = new Movie { Id = Guid.NewGuid(), Name = "Movie", OriginalTitle = null };

        _service.SaveItems([movie], CancellationToken.None);

        Assert.Null(ReadCleanOriginalTitle(movie.Id));
    }

    [Fact]
    public void SaveItems_OriginalTitleChanged_StoresTheNewNormalizedTitle()
    {
        var movie = new Movie { Id = Guid.NewGuid(), Name = "Movie", OriginalTitle = "Émile Zola" };
        _service.SaveItems([movie], CancellationToken.None);

        movie.OriginalTitle = "Amélie Poulain";
        _service.SaveItems([movie], CancellationToken.None);

        Assert.Equal("amelie poulain", ReadCleanOriginalTitle(movie.Id));
    }

    [Fact]
    public void SaveItems_OriginalTitleSetToNull_StoresNull()
    {
        var movie = new Movie { Id = Guid.NewGuid(), Name = "Movie", OriginalTitle = "Émile Zola" };
        _service.SaveItems([movie], CancellationToken.None);

        movie.OriginalTitle = null;
        _service.SaveItems([movie], CancellationToken.None);

        Assert.Null(ReadCleanOriginalTitle(movie.Id));
    }

    [Fact]
    public void SaveItems_OriginalTitleWithNul_StoresTheTitleAndItsNormalizedFormWithoutIt()
    {
        var movie = new Movie { Id = Guid.NewGuid(), Name = "Movie", OriginalTitle = "Emi\0le" };

        _service.SaveItems([movie], CancellationToken.None);

        using var context = CreateDbContext();
        var row = context.BaseItems.AsNoTracking().Single(e => e.Id.Equals(movie.Id));
        Assert.Equal("Emile", row.OriginalTitle);
        Assert.Equal("emile", row.CleanOriginalTitle);
    }

    [Fact]
    public void UpdatePeople_NewPerson_StoresTheNameInLowerCase()
    {
        var itemId = AddMovie();

        _people.UpdatePeople(itemId, [new PersonInfo { Name = "Émile ZOLA", Type = PersonKind.Actor }]);

        using var context = CreateDbContext();
        var person = Assert.Single(context.Peoples.AsNoTracking());
        Assert.Equal("Émile ZOLA", person.Name);
        Assert.Equal("émile zola", person.NameLower);
    }

    [Fact]
    public void UpdatePeople_PersonKnownUnderAnotherCase_KeepsOneRowWithItsLowerCaseName()
    {
        var itemId = AddMovie();
        _people.UpdatePeople(itemId, [new PersonInfo { Name = "Émile Zola", Type = PersonKind.Actor }]);

        _people.UpdatePeople(itemId, [new PersonInfo { Name = "ÉMILE ZOLA", Type = PersonKind.Actor }]);

        using var context = CreateDbContext();
        var person = Assert.Single(context.Peoples.AsNoTracking());
        Assert.Equal("Émile Zola", person.Name);
        Assert.Equal("émile zola", person.NameLower);
    }

    [Fact]
    public void UpdatePeople_PersonOfAnotherTypeAndCase_ReusesNothingAndStoresItsOwnLowerCaseName()
    {
        var itemId = AddMovie();
        _people.UpdatePeople(itemId, [
            new PersonInfo { Name = "Émile Zola", Type = PersonKind.Actor },
            new PersonInfo { Name = "ÉMILE ZOLA", Type = PersonKind.Director }
        ]);

        using var context = CreateDbContext();
        var people = context.Peoples.AsNoTracking().OrderBy(e => e.PersonType).ToList();
        Assert.Equal(2, people.Count);
        Assert.All(people, e => Assert.Equal("émile zola", e.NameLower));
    }

    private string? ReadCleanOriginalTitle(Guid id)
    {
        using var context = CreateDbContext();
        return context.BaseItems.AsNoTracking().Where(e => e.Id.Equals(id)).Select(e => e.CleanOriginalTitle).Single();
    }

    private Guid AddMovie()
    {
        var id = Guid.NewGuid();
        using var context = CreateDbContext();
        context.BaseItems.Add(new BaseItemEntity
        {
            Id = id,
            Type = new ItemTypeLookup().BaseItemKindNames[BaseItemKind.Movie],
            Name = "Movie",
            MediaType = "Video",
            IsMovie = true,
            IsFolder = false,
            IsVirtualItem = false
        });
        context.SaveChanges();
        return id;
    }
}
