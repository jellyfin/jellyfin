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
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.Entities;
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
