using System;
using System.Collections.Generic;
using System.Linq;
using Emby.Server.Implementations.Dto;
using MediaBrowser.Common;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Trickplay;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Dto;

public class DtoServiceTests
{
    private readonly Mock<ILibraryManager> _libraryManagerMock;
    private readonly DtoService _dtoService;

    public DtoServiceTests()
    {
        _libraryManagerMock = new Mock<ILibraryManager>();

        var imageProcessor = new Mock<IImageProcessor>();
        // Deterministic tag derived from the image so each item gets a distinct, assertable tag.
        imageProcessor
            .Setup(x => x.GetImageCacheTag(It.IsAny<BaseItem>(), It.IsAny<ItemImageInfo>()))
            .Returns((BaseItem _, ItemImageInfo image) => "tag:" + image.Path);

        var appHost = new Mock<IApplicationHost>();
        appHost.Setup(x => x.SystemId).Returns("test-server");

        // Video.SourceType probes the active-recording manager; provide one so it doesn't NRE.
        Video.RecordingsManager = new Mock<IRecordingsManager>().Object;

        _dtoService = new DtoService(
            NullLogger<DtoService>.Instance,
            _libraryManagerMock.Object,
            new Mock<IUserDataManager>().Object,
            imageProcessor.Object,
            new Mock<IProviderManager>().Object,
            new Mock<IRecordingsManager>().Object,
            appHost.Object,
            new Mock<IMediaSourceManager>().Object,
            new Lazy<ILiveTvManager>(() => new Mock<ILiveTvManager>().Object),
            new Mock<ITrickplayManager>().Object,
            new Mock<IChapterManager>().Object);

        // Episode.Series / Episode.Season resolve through the static BaseItem.LibraryManager.
        BaseItem.LibraryManager = _libraryManagerMock.Object;
    }

    [Fact]
    public void GetBaseItemDto_Episode_AttachesSeasonPosterAsParentPrimaryImage()
    {
        var (episode, season, _) = BuildEpisode(seasonHasPoster: true);
        var options = new DtoOptions(false) { Fields = [ItemFields.PrimaryImageAspectRatio] };

        var dto = _dtoService.GetBaseItemDto(episode, options);

        // The season poster is attached additively; the episode keeps its own primary and 16:9 ratio,
        // and clients decide per view whether to prefer the parent/series poster over the episode still.
        Assert.NotNull(dto.ImageTags);
        Assert.True(dto.ImageTags.ContainsKey(ImageType.Primary));
        Assert.NotNull(dto.SeriesPrimaryImageTag);
        Assert.Equal(season.Id, dto.ParentPrimaryImageItemId);
        Assert.Equal("tag:" + season.GetImageInfo(ImageType.Primary, 0)!.Path, dto.ParentPrimaryImageTag);
        // Aspect ratio stays the episode's own image, not the poster's.
        Assert.Equal(episode.GetDefaultPrimaryImageAspectRatio(), dto.PrimaryImageAspectRatio);
    }

    [Fact]
    public void GetBaseItemDto_Episode_ParentPrimaryImageFallsBackToSeriesWhenSeasonHasNoPoster()
    {
        var (episode, _, series) = BuildEpisode(seasonHasPoster: false);
        var options = new DtoOptions(false);

        var dto = _dtoService.GetBaseItemDto(episode, options);

        // Episode image is retained; ParentPrimaryImage falls back to the series poster.
        Assert.NotNull(dto.ImageTags);
        Assert.True(dto.ImageTags.ContainsKey(ImageType.Primary));
        Assert.NotNull(dto.SeriesPrimaryImageTag);
        Assert.Equal(series.Id, dto.ParentPrimaryImageItemId);
        Assert.Equal("tag:" + series.GetImageInfo(ImageType.Primary, 0)!.Path, dto.ParentPrimaryImageTag);
    }

    [Fact]
    public void GetBaseItemDto_Episode_WithoutParentPosters_KeepsOnlyEpisodePrimary()
    {
        var (episode, _, _) = BuildEpisode(seasonHasPoster: false, seriesHasPoster: false);
        var options = new DtoOptions(false);

        var dto = _dtoService.GetBaseItemDto(episode, options);

        // With no season or series poster there is nothing to attach; the episode keeps its own primary.
        Assert.NotNull(dto.ImageTags);
        Assert.True(dto.ImageTags.ContainsKey(ImageType.Primary));
        Assert.Null(dto.ParentPrimaryImageItemId);
    }

    [Fact]
    public void GetBaseItemDto_GenreWithoutALink_IsStillReported()
    {
        // A name that failed to resolve is not the same as the genre being gone: dropping it would
        // take the genre off the item entirely while dto.Genres still lists it.
        var movie = BuildMovie("Sci-Fi", "Noir");
        var linkedId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
        WithLinks(movie.Id, [new NameGuidPair { Id = linkedId, Name = "Sci-Fi" }]);

        var dto = _dtoService.GetBaseItemDto(movie, new DtoOptions(false) { Fields = [ItemFields.Genres] });

        Assert.Collection(
            dto.GenreItems!,
            e => Assert.Equal(linkedId, e.Id),
            e =>
            {
                Assert.Equal("Noir", e.Name);
                Assert.Equal(HashedId("Noir"), e.Id);
            });
    }

    [Fact]
    public void GetBaseItemDto_RenamedGenre_ReportsTheLinkRatherThanBothSpellings()
    {
        // No link matches "Sci-Fi" any more, so the genre item was renamed and the link is the only
        // truthful answer. Reporting the stale spelling next to it would show the genre twice.
        var movie = BuildMovie("Sci-Fi");
        var linkedId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
        WithLinks(movie.Id, [new NameGuidPair { Id = linkedId, Name = "Science Fiction" }]);

        var dto = _dtoService.GetBaseItemDto(movie, new DtoOptions(false) { Fields = [ItemFields.Genres] });

        var only = Assert.Single(dto.GenreItems!);
        Assert.Equal(linkedId, only.Id);
        Assert.Equal("Science Fiction", only.Name);
    }

    [Fact]
    public void GetBaseItemDto_SpellingsThatCleanAlike_ReportTheOneLink()
    {
        var movie = BuildMovie("Sci Fi");
        var linkedId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000003");
        WithLinks(movie.Id, [new NameGuidPair { Id = linkedId, Name = "Sci-Fi" }]);

        var dto = _dtoService.GetBaseItemDto(movie, new DtoOptions(false) { Fields = [ItemFields.Genres] });

        Assert.Equal(linkedId, Assert.Single(dto.GenreItems!).Id);
    }

    [Fact]
    public void GetBaseItemDto_NoLinksAtAll_FallsBackToTheNamesTheItemCarries()
    {
        var movie = BuildMovie("Sci-Fi", "Noir");

        var dto = _dtoService.GetBaseItemDto(movie, new DtoOptions(false) { Fields = [ItemFields.Genres] });

        Assert.Equal(["Sci-Fi", "Noir"], dto.GenreItems!.Select(e => e.Name));
        Assert.Equal([HashedId("Sci-Fi"), HashedId("Noir")], dto.GenreItems!.Select(e => e.Id));
    }

    [Fact]
    public void GetItemByNameDto_DoesNotLookUpLinksItCannotHave()
    {
        // This runs once per row of a /Genres or /Artists page, and a by-name item carries no genres
        // or studios of its own, so the lookup could only ever come back with nothing.
        var genre = new Genre { Id = Guid.NewGuid(), Name = "Sci-Fi" };

        _dtoService.GetItemByNameDto(genre, new DtoOptions(false) { Fields = [ItemFields.Genres, ItemFields.Studios] }, null);

        _libraryManagerMock.Verify(x => x.GetItemByNameLinks(It.IsAny<IReadOnlyList<Guid>>()), Times.Never);
    }

    // Stands in for the id a by-name item of this name would hash to. How the library manager derives
    // that is its own business and is mocked out here; all this needs is to be the same every time.
    private static Guid HashedId(string name)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(bytes, name.GetHashCode(StringComparison.Ordinal));

        return new Guid(bytes);
    }

    private Movie BuildMovie(params string[] genres)
    {
        var movie = new Movie { Id = Guid.NewGuid(), Name = "Movie", Genres = genres };

        _libraryManagerMock.Setup(x => x.GetGenreId(It.IsAny<string>())).Returns((string name) => HashedId(name));
        _libraryManagerMock.Setup(x => x.GetMusicGenreId(It.IsAny<string>())).Returns((string name) => HashedId(name));

        // An item with no links, rather than the null an unconfigured mock hands back for a lookup that
        // never returns null in production. WithLinks replaces this for the tests that need links.
        _libraryManagerMock
            .Setup(x => x.GetItemByNameLinks(It.IsAny<IReadOnlyList<Guid>>()))
            .Returns(new Dictionary<Guid, ItemByNameLinks>());

        return movie;
    }

    private void WithLinks(Guid itemId, IReadOnlyList<NameGuidPair> genres)
    {
        _libraryManagerMock
            .Setup(x => x.GetItemByNameLinks(It.IsAny<IReadOnlyList<Guid>>()))
            .Returns(new Dictionary<Guid, ItemByNameLinks> { [itemId] = new ItemByNameLinks(genres, []) });
    }

    private (Episode Episode, Season Season, Series Series) BuildEpisode(bool seasonHasPoster, bool seriesHasPoster = true)
    {
        // Non-local (http) paths keep aspect-ratio resolution off the image processor and on the
        // item's default ratio, which is portrait (2/3) for Season/Series and 16:9 for Episode.
        var series = new Series { Id = Guid.NewGuid(), Name = "Series" };
        if (seriesHasPoster)
        {
            series.SetImage(new ItemImageInfo { Type = ImageType.Primary, Path = "http://test/series.jpg" }, 0);
        }

        var season = new Season { Id = Guid.NewGuid(), Name = "Season", SeriesId = series.Id };
        if (seasonHasPoster)
        {
            season.SetImage(new ItemImageInfo { Type = ImageType.Primary, Path = "http://test/season.jpg" }, 0);
        }

        var episode = new Episode
        {
            Id = Guid.NewGuid(),
            Name = "Episode",
            SeasonId = season.Id,
            SeriesId = series.Id
        };
        episode.SetImage(new ItemImageInfo { Type = ImageType.Primary, Path = "http://test/episode.jpg" }, 0);

        _libraryManagerMock.Setup(x => x.GetItemById(season.Id)).Returns(season);
        _libraryManagerMock.Setup(x => x.GetItemById(series.Id)).Returns(series);

        return (episode, season, series);
    }
}
