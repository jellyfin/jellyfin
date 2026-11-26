using System;
using System.Collections.Generic;
using Emby.Server.Implementations.Dto;
using Emby.Server.Implementations.Playlists;
using Jellyfin.Data.Enums;
using MediaBrowser.Common;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Dto;

public class DtoServiceImageInheritanceTests
{
    [Fact]
    public void GetBaseItemDto_PlaylistsUserViewWithDisplayParentPrimary_UsesDisplayParentPrimaryImage()
    {
        var displayParent = new PlaylistsFolder
        {
            Id = Guid.NewGuid(),
            ImageInfos =
            [
                new ItemImageInfo
                {
                    Type = ImageType.Primary,
                    Path = "/images/playlists-custom.jpg",
                    DateModified = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc)
                }
            ]
        };

        var userView = new UserView
        {
            Id = Guid.NewGuid(),
            ViewType = CollectionType.playlists,
            DisplayParentId = displayParent.Id,
            ImageInfos =
            [
                new ItemImageInfo
                {
                    Type = ImageType.Primary,
                    Path = "/images/generated.png",
                    DateModified = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc)
                }
            ]
        };

        var dtoService = BuildDtoService(displayParent);

        var dto = dtoService.GetBaseItemDto(userView, new DtoOptions(false));

        Assert.NotNull(dto.ParentPrimaryImageItemId);
        Assert.Equal(displayParent.Id, dto.ParentPrimaryImageItemId);
        Assert.Equal("/images/playlists-custom.jpg", dto.ParentPrimaryImageTag);
        Assert.False(dto.ImageTags?.ContainsKey(ImageType.Primary));
    }

    [Fact]
    public void GetBaseItemDto_PlaylistsUserViewWithoutDisplayParentPrimary_KeepsOwnPrimaryImage()
    {
        var displayParent = new PlaylistsFolder
        {
            Id = Guid.NewGuid(),
            ImageInfos = []
        };

        var userView = new UserView
        {
            Id = Guid.NewGuid(),
            ViewType = CollectionType.playlists,
            DisplayParentId = displayParent.Id,
            ImageInfos =
            [
                new ItemImageInfo
                {
                    Type = ImageType.Primary,
                    Path = "/images/generated.png",
                    DateModified = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc)
                }
            ]
        };

        var dtoService = BuildDtoService(displayParent);

        var dto = dtoService.GetBaseItemDto(userView, new DtoOptions(false));

        Assert.Null(dto.ParentPrimaryImageItemId);
        Assert.Null(dto.ParentPrimaryImageTag);
        Assert.NotNull(dto.ImageTags);
        Assert.True(dto.ImageTags.ContainsKey(ImageType.Primary));
        Assert.Equal("/images/generated.png", dto.ImageTags[ImageType.Primary]);
    }

    [Fact]
    public void GetBaseItemDtos_MusicAlbums_ResolveInheritedThumbFromArtistBatch_WithoutPerAlbumLookup()
    {
        var artist = new MusicArtist
        {
            Id = Guid.NewGuid(),
            Name = "Some Artist",
            ImageInfos =
            [
                new ItemImageInfo
                {
                    Type = ImageType.Thumb,
                    Path = "/images/artist-thumb.jpg",
                    DateModified = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            ]
        };

        static MusicAlbum MakeAlbum() => new MusicAlbum
        {
            Id = Guid.NewGuid(),
            Name = "Album",
            AlbumArtists = ["Some Artist"],
            ImageInfos = []
        };

        var libraryManager = new Mock<ILibraryManager>();

        // DtoService resolves every album-artist name in ONE batch (GetArtists). The album's inherited
        // Thumb/Backdrop images must come from that batch, not a per-album GetArtist/GetItemList lookup
        // (the N+1). GetArtist is intentionally left unset: a regression to the per-album path would
        // resolve no artist and fail the assertions below.
        libraryManager
            .Setup(x => x.GetArtists(It.IsAny<IReadOnlyList<string>>()))
            .Returns(new Dictionary<string, MusicArtist[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Some Artist"] = [artist]
            });

        var dtoService = BuildDtoService(libraryManager);

        var dtos = dtoService.GetBaseItemDtos([MakeAlbum(), MakeAlbum()], new DtoOptions(false));

        Assert.Equal(2, dtos.Count);
        foreach (var dto in dtos)
        {
            Assert.Equal(artist.Id, dto.ParentThumbItemId);
            Assert.Equal("/images/artist-thumb.jpg", dto.ParentThumbImageTag);
        }

        // The artist lookup is batched once for the whole set, never once per album.
        libraryManager.Verify(x => x.GetArtists(It.IsAny<IReadOnlyList<string>>()), Times.Once);
        libraryManager.Verify(x => x.GetArtist(It.IsAny<string>(), It.IsAny<DtoOptions>()), Times.Never);
    }

    [Fact]
    public void GetBaseItemDtos_Items_ResolvePeopleFromBatch_WithoutPerItemLookup()
    {
        static MusicAlbum MakeAlbum() => new MusicAlbum
        {
            Id = Guid.NewGuid(),
            Name = "Album",
            ImageInfos = []
        };

        var albumOne = MakeAlbum();
        var albumTwo = MakeAlbum();

        var libraryManager = new Mock<ILibraryManager>();

        // DtoService resolves people for every item in ONE batch (GetPeopleByItems) before the
        // per-item loop. A regression to the per-item path would call GetPeople(BaseItem) once per
        // item (the N+1); it is intentionally left unset so such a regression fails here.
        libraryManager
            .Setup(x => x.GetPeopleByItems(It.IsAny<IReadOnlyList<Guid>>()))
            .Returns(new Dictionary<Guid, IReadOnlyList<PersonInfo>>
            {
                [albumOne.Id] = [new PersonInfo { ItemId = albumOne.Id, Name = "Some Actor", Type = PersonKind.Actor }],
                [albumTwo.Id] = [new PersonInfo { ItemId = albumTwo.Id, Name = "Some Actor", Type = PersonKind.Actor }]
            });

        // AttachPeople resolves the names to their Person entities in one batch too (GetPersons).
        // GetPerson is intentionally left unset: a regression to the per-name lookup would resolve
        // no person and fail the assertions below.
        libraryManager
            .Setup(x => x.GetPersons(It.IsAny<IReadOnlyCollection<string>>()))
            .Returns(new Dictionary<string, Person>(StringComparer.OrdinalIgnoreCase)
            {
                ["Some Actor"] = new Person { Id = Guid.NewGuid(), Name = "Some Actor" }
            });

        var dtoService = BuildDtoService(libraryManager);

        var options = new DtoOptions(false) { Fields = [ItemFields.People] };
        var dtos = dtoService.GetBaseItemDtos([albumOne, albumTwo], options);

        Assert.Equal(2, dtos.Count);
        foreach (var dto in dtos)
        {
            Assert.NotNull(dto.People);
            Assert.Single(dto.People);
            Assert.Equal("Some Actor", dto.People[0].Name);
        }

        // People are batched once for the whole set, never once per item, and so are their Person entities.
        libraryManager.Verify(x => x.GetPeopleByItems(It.IsAny<IReadOnlyList<Guid>>()), Times.Once);
        libraryManager.Verify(x => x.GetPeople(It.IsAny<BaseItem>()), Times.Never);
        libraryManager.Verify(x => x.GetPersons(It.IsAny<IReadOnlyCollection<string>>()), Times.Once);
        libraryManager.Verify(x => x.GetPerson(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetBaseItemDtos_People_ResolveEveryNameOnThePageInOneLookup()
    {
        var albumOne = new MusicAlbum { Id = Guid.NewGuid(), Name = "Album", ImageInfos = [] };
        var albumTwo = new MusicAlbum { Id = Guid.NewGuid(), Name = "Album", ImageInfos = [] };

        var actor = new Person
        {
            Id = Guid.NewGuid(),
            Name = "Some Actor",
            ImageInfos =
            [
                new ItemImageInfo
                {
                    Type = ImageType.Primary,
                    Path = "/images/some-actor.jpg",
                    DateModified = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            ]
        };
        var director = new Person { Id = Guid.NewGuid(), Name = "A Director", ImageInfos = [] };
        var writer = new Person { Id = Guid.NewGuid(), Name = "A Writer", ImageInfos = [] };

        var libraryManager = new Mock<ILibraryManager>();

        // The same actor credited on both items, once in another case, beside a name each item has alone.
        libraryManager
            .Setup(x => x.GetPeopleByItems(It.IsAny<IReadOnlyList<Guid>>()))
            .Returns(new Dictionary<Guid, IReadOnlyList<PersonInfo>>
            {
                [albumOne.Id] =
                [
                    new PersonInfo { ItemId = albumOne.Id, Name = "Some Actor", Type = PersonKind.Actor },
                    new PersonInfo { ItemId = albumOne.Id, Name = "A Director", Type = PersonKind.Director }
                ],
                [albumTwo.Id] =
                [
                    new PersonInfo { ItemId = albumTwo.Id, Name = "some actor", Type = PersonKind.Actor },
                    new PersonInfo { ItemId = albumTwo.Id, Name = "A Writer", Type = PersonKind.Writer }
                ]
            });

        IReadOnlyCollection<string>? asked = null;
        libraryManager
            .Setup(x => x.GetPersons(It.IsAny<IReadOnlyCollection<string>>()))
            .Callback<IReadOnlyCollection<string>>(names => asked = names)
            .Returns(new Dictionary<string, Person>(StringComparer.OrdinalIgnoreCase)
            {
                [actor.Name] = actor,
                [director.Name] = director,
                [writer.Name] = writer
            });

        var dtoService = BuildDtoService(libraryManager);

        var options = new DtoOptions(false) { Fields = [ItemFields.People] };
        var dtos = dtoService.GetBaseItemDtos([albumOne, albumTwo], options);

        // One lookup for the page, each distinct name in it once, whatever its case.
        libraryManager.Verify(x => x.GetPersons(It.IsAny<IReadOnlyCollection<string>>()), Times.Once);
        libraryManager.Verify(x => x.GetPerson(It.IsAny<string>()), Times.Never);
        Assert.NotNull(asked);
        Assert.Equal(3, asked.Count);
        Assert.Equal(
            new HashSet<string>(["Some Actor", "A Director", "A Writer"], StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(asked, StringComparer.OrdinalIgnoreCase));

        // Each credit takes its id and picture from the entity the batch resolved its name to.
        Assert.Equal(2, dtos.Count);
        Assert.NotNull(dtos[0].People);
        Assert.NotNull(dtos[1].People);
        var firstActor = Assert.Single(dtos[0].People, p => p.Type == PersonKind.Actor);
        var secondActor = Assert.Single(dtos[1].People, p => p.Type == PersonKind.Actor);
        Assert.Equal(actor.Id, firstActor.Id);
        Assert.Equal(actor.Id, secondActor.Id);
        Assert.Equal("/images/some-actor.jpg", firstActor.PrimaryImageTag);
        Assert.Equal(director.Id, Assert.Single(dtos[0].People, p => p.Type == PersonKind.Director).Id);
        Assert.Equal(writer.Id, Assert.Single(dtos[1].People, p => p.Type == PersonKind.Writer).Id);
    }

    [Fact]
    public void GetBaseItemDtos_People_NameTheBatchDidNotFind_IsLeftOut()
    {
        var album = new MusicAlbum { Id = Guid.NewGuid(), Name = "Album", ImageInfos = [] };

        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(x => x.GetPeopleByItems(It.IsAny<IReadOnlyList<Guid>>()))
            .Returns(new Dictionary<Guid, IReadOnlyList<PersonInfo>>
            {
                [album.Id] =
                [
                    new PersonInfo { ItemId = album.Id, Name = "Some Actor", Type = PersonKind.Actor },
                    new PersonInfo { ItemId = album.Id, Name = "Nobody", Type = PersonKind.Actor }
                ]
            });

        // "Nobody" has no Person entity, as GetPerson would answer null for it on the per-name path.
        libraryManager
            .Setup(x => x.GetPersons(It.IsAny<IReadOnlyCollection<string>>()))
            .Returns(new Dictionary<string, Person>(StringComparer.OrdinalIgnoreCase)
            {
                ["Some Actor"] = new Person { Id = Guid.NewGuid(), Name = "Some Actor" }
            });

        var dtoService = BuildDtoService(libraryManager);

        var options = new DtoOptions(false) { Fields = [ItemFields.People] };
        var dto = Assert.Single(dtoService.GetBaseItemDtos([album], options));

        Assert.NotNull(dto.People);
        Assert.Equal("Some Actor", Assert.Single(dto.People).Name);
        libraryManager.Verify(x => x.GetPerson(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void GetBaseItemDto_SingleItem_ResolvesPeopleByName()
    {
        var album = new MusicAlbum { Id = Guid.NewGuid(), Name = "Album", ImageInfos = [] };
        var actor = new Person { Id = Guid.NewGuid(), Name = "Some Actor" };

        var libraryManager = new Mock<ILibraryManager>();

        // A single item has no page to batch, so it keeps the per-item and per-name lookups.
        libraryManager
            .Setup(x => x.GetPeople(album))
            .Returns([new PersonInfo { ItemId = album.Id, Name = "Some Actor", Type = PersonKind.Actor }]);
        libraryManager
            .Setup(x => x.GetPerson("Some Actor"))
            .Returns(actor);

        var dtoService = BuildDtoService(libraryManager);

        var dto = dtoService.GetBaseItemDto(album, new DtoOptions(false) { Fields = [ItemFields.People] });

        Assert.NotNull(dto.People);
        Assert.Equal(actor.Id, Assert.Single(dto.People).Id);
        libraryManager.Verify(x => x.GetPersons(It.IsAny<IReadOnlyCollection<string>>()), Times.Never);
    }

    [Fact]
    public void GetBaseItemDtos_Videos_ResolveMediaSourceCountFromBatch_WithoutPerItemLookup()
    {
        static Movie MakeMovie() => new Movie
        {
            Id = Guid.NewGuid(),
            Name = "Movie",
            ImageInfos = []
        };

        var movieOne = MakeMovie();
        var movieTwo = MakeMovie();

        var libraryManager = new Mock<ILibraryManager>();

        // DtoService detects which videos own alternate versions in ONE batch
        // (GetItemIdsWithAlternateVersions) before the per-item loop. Videos absent from that set have a
        // single media source, so the per-item GetLinkedAlternateVersions/GetLocalAlternateVersionIds
        // queries (the N+1) must be skipped entirely. Here neither movie has alternate versions.
        libraryManager
            .Setup(x => x.GetItemIdsWithAlternateVersions(It.IsAny<IReadOnlyList<Guid>>()))
            .Returns(new HashSet<Guid>());

        var dtoService = BuildDtoService(libraryManager);

        var options = new DtoOptions(false) { Fields = [ItemFields.MediaSourceCount] };
        var dtos = dtoService.GetBaseItemDtos([movieOne, movieTwo], options);

        Assert.Equal(2, dtos.Count);

        // A single media source is the default, so the count is left unset (the client treats null as one).
        foreach (var dto in dtos)
        {
            Assert.Null(dto.MediaSourceCount);
        }

        // The alternate-version check is batched once for the whole set, and the per-item lookups are
        // never reached because the batch already ruled out alternate versions.
        libraryManager.Verify(x => x.GetItemIdsWithAlternateVersions(It.IsAny<IReadOnlyList<Guid>>()), Times.Once);
        libraryManager.Verify(x => x.GetLinkedAlternateVersions(It.IsAny<Video>()), Times.Never);
        libraryManager.Verify(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>()), Times.Never);
    }

    [Fact]
    public void GetBaseItemDtos_VideoInAlternateVersionBatch_ResolvesRealCount()
    {
        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            Name = "Movie",
            ImageInfos = []
        };

        var libraryManager = new Mock<ILibraryManager>();

        // This movie IS in the batch set, so the fast path must not short-circuit it: the per-item
        // lookups still run and the count is computed exactly as it was before batching. Two linked
        // alternate versions plus the movie itself is a count of three.
        libraryManager
            .Setup(x => x.GetItemIdsWithAlternateVersions(It.IsAny<IReadOnlyList<Guid>>()))
            .Returns(new HashSet<Guid> { movie.Id });
        libraryManager
            .Setup(x => x.GetLinkedAlternateVersions(It.IsAny<Video>()))
            .Returns([new Movie { Id = Guid.NewGuid() }, new Movie { Id = Guid.NewGuid() }]);
        libraryManager
            .Setup(x => x.GetLocalAlternateVersionIds(It.IsAny<Video>()))
            .Returns([]);

        var dtoService = BuildDtoService(libraryManager);

        var options = new DtoOptions(false) { Fields = [ItemFields.MediaSourceCount] };
        var dtos = dtoService.GetBaseItemDtos([movie], options);

        Assert.Single(dtos);
        Assert.Equal(3, dtos[0].MediaSourceCount);
        libraryManager.Verify(x => x.GetItemIdsWithAlternateVersions(It.IsAny<IReadOnlyList<Guid>>()), Times.Once);
    }

    private static DtoService BuildDtoService(BaseItem displayParent)
    {
        var libraryManager = new Mock<ILibraryManager>();
        libraryManager
            .Setup(x => x.GetItemById(displayParent.Id))
            .Returns(displayParent);
        return BuildDtoService(libraryManager);
    }

    private static DtoService BuildDtoService(Mock<ILibraryManager> libraryManager)
    {
        var userDataManager = new Mock<IUserDataManager>();
        var imageProcessor = new Mock<IImageProcessor>();
        var providerManager = new Mock<IProviderManager>();
        var recordingsManager = new Mock<IRecordingsManager>();
        var appHost = new Mock<IApplicationHost>();
        var mediaSourceManager = new Mock<IMediaSourceManager>();
        var liveTvManager = new Mock<ILiveTvManager>();
        var trickplayManager = new Mock<ITrickplayManager>();
        var chapterManager = new Mock<IChapterManager>();
        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<DtoService>>();

        imageProcessor
            .Setup(x => x.GetImageCacheTag(It.IsAny<BaseItem>(), It.IsAny<ItemImageInfo>()))
            .Returns<BaseItem, ItemImageInfo>((_, image) => image.Path);

        // Video.IsActiveRecording() dereferences this static during DTO building.
        Video.RecordingsManager = recordingsManager.Object;
        BaseItem.LibraryManager = libraryManager.Object;

        return new DtoService(
            logger.Object,
            libraryManager.Object,
            userDataManager.Object,
            imageProcessor.Object,
            providerManager.Object,
            recordingsManager.Object,
            appHost.Object,
            mediaSourceManager.Object,
            new Lazy<ILiveTvManager>(() => liveTvManager.Object),
            trickplayManager.Object,
            chapterManager.Object);
    }
}
