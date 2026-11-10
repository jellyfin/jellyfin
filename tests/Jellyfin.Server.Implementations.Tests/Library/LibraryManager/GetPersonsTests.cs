using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using Emby.Naming.Common;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Controller.Sorting;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.LibraryManager;

/// <summary>
/// Tests for resolving many people's names to their Person items at once: a page of items can credit
/// thousands of people, and looking each up on its own is one database query per name.
/// </summary>
[Collection("LibraryManagerTests")]
public sealed class GetPersonsTests : IDisposable
{
    private readonly Emby.Server.Implementations.Library.LibraryManager _libraryManager;
    private readonly Mock<IItemRepository> _itemRepository;
    private readonly IServerConfigurationManager? _previousConfigurationManager;
    private readonly IFileSystem? _previousFileSystem;

    public GetPersonsTests()
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());
        fixture.Register(() => new NamingOptions());
        var configurationManager = fixture.Freeze<Mock<IServerConfigurationManager>>();
        configurationManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());
        configurationManager.Setup(c => c.ApplicationPaths.ProgramDataPath).Returns("/data");
        configurationManager.Setup(c => c.ApplicationPaths.PeoplePath).Returns("/data/metadata/People");
        _itemRepository = fixture.Freeze<Mock<IItemRepository>>();

        _libraryManager = fixture.Build<Emby.Server.Implementations.Library.LibraryManager>()
            .Do(s => s.AddParts(
                fixture.Create<IEnumerable<IResolverIgnoreRule>>(),
                [],
                fixture.Create<IEnumerable<IIntroProvider>>(),
                fixture.Create<IEnumerable<IBaseItemComparer>>(),
                fixture.Create<IEnumerable<ILibraryPostScanTask>>()))
            .Create();

        // Person.GetPath, which a person's id is derived from, reads the people folder and makes the
        // name a folder name through these process-wide statics; restored in Dispose.
        _previousConfigurationManager = BaseItem.ConfigurationManager;
        _previousFileSystem = BaseItem.FileSystem;
        BaseItem.ConfigurationManager = configurationManager.Object;
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(f => f.GetValidFilename(It.IsAny<string>())).Returns<string>(name => name);
        BaseItem.FileSystem = fileSystem.Object;
    }

    public void Dispose()
    {
        BaseItem.ConfigurationManager = _previousConfigurationManager!;
        BaseItem.FileSystem = _previousFileSystem!;
    }

    [Fact]
    public void GetPersons_Names_AsksTheRepositoryOnceForTheirPersonItems()
    {
        InternalItemsQuery? asked = null;
        _itemRepository
            .Setup(r => r.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Callback<InternalItemsQuery>(query => asked = query)
            .Returns([]);

        _libraryManager.GetPersons(["Some Actor", "A Director"]);

        _itemRepository.Verify(r => r.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Once);
        Assert.NotNull(asked);

        // By the ids the names derive, the same ones GetPerson looks a single name up by.
        Assert.Equal(
            new HashSet<Guid> { _libraryManager.GetPersonId("Some Actor"), _libraryManager.GetPersonId("A Director") },
            new HashSet<Guid>(asked.ItemIds));

        // People only, with the images their picture tags come from, and none of the user data
        // or linked children a query of any type would join for every row.
        Assert.Equal([BaseItemKind.Person], asked.IncludeItemTypes);
        Assert.True(asked.DtoOptions.EnableImages);
        Assert.False(asked.DtoOptions.EnableUserData);
    }

    [Fact]
    public void GetPersons_Found_AreKeyedByNameInAnyCase()
    {
        var person = new Person { Id = Guid.NewGuid(), Name = "Some Actor" };
        _itemRepository
            .Setup(r => r.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns([person]);

        var persons = _libraryManager.GetPersons(["some actor"]);

        Assert.Same(person, persons["Some Actor"]);
        Assert.Same(person, persons["SOME ACTOR"]);
    }

    [Fact]
    public void GetPersons_NamesDifferingOnlyInCase_KeepOne()
    {
        // Two items whose names differ only in case would collide as dictionary keys.
        _itemRepository
            .Setup(r => r.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns([
                new Person { Id = Guid.NewGuid(), Name = "Some Actor" },
                new Person { Id = Guid.NewGuid(), Name = "some actor" }
            ]);

        var persons = _libraryManager.GetPersons(["Some Actor", "some actor"]);

        Assert.Single(persons);
    }

    [Fact]
    public void GetPersons_NoNames_AsksNothing()
    {
        var persons = _libraryManager.GetPersons([]);

        Assert.Empty(persons);
        _itemRepository.Verify(r => r.GetItemList(It.IsAny<InternalItemsQuery>()), Times.Never);
    }
}
