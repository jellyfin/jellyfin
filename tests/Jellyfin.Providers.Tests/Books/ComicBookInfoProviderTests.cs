using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Books.ComicBookInfo;
using MediaBrowser.Providers.Books.ComicBookInfo.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.Books;

public sealed class ComicBookInfoProviderTests : IDisposable
{
    private const string ValidComment = """
        {"appID":"test","ComicBookInfo/1.0":{"series":"Jungle Juice","title":"Episode 36","issue":175}}
        """;

    private readonly string _directory;
    private readonly ComicBookInfoFormat _comicBookInfoFormat;

    public ComicBookInfoProviderTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "jf-cbz-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);

        _comicBookInfoFormat = GenerateTestData();
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
    }

    [Theory]
    [InlineData(null)] // archive written without ever touching Comment
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReadMetadata_EmptyArchiveComment_SkipsWithoutDeserializing(string? comment)
    {
        var logger = new Mock<ILogger<ComicBookInfoProvider>>();
        var path = CreateArchive(comment);
        var provider = new ComicBookInfoProvider(CreateFileSystem(path), logger.Object);

        var result = await provider.ReadMetadata(new ItemInfo(new Book { Path = path }), Mock.Of<IDirectoryService>(), CancellationToken.None);

        Assert.False(result.HasMetadata);
        VerifyLogged(logger, LogLevel.Debug, "missing ComicBookInfo in archive comment");
        VerifyNothingLoggedAbove(logger, LogLevel.Debug);
    }

    [Fact]
    public async Task ReadMetadata_ArchiveCommentIsNotComicBookInfo_SkipsWithoutError()
    {
        var logger = new Mock<ILogger<ComicBookInfoProvider>>();
        var path = CreateArchive("Created by some packer");
        var provider = new ComicBookInfoProvider(CreateFileSystem(path), logger.Object);

        var result = await provider.ReadMetadata(new ItemInfo(new Book { Path = path }), Mock.Of<IDirectoryService>(), CancellationToken.None);

        Assert.False(result.HasMetadata);
        VerifyLogged(logger, LogLevel.Debug, "archive comment is not valid ComicBookInfo metadata");
        VerifyNothingLoggedAbove(logger, LogLevel.Debug);
    }

    [Fact]
    public async Task ReadMetadata_ValidComicBookInfoComment_ReturnsMetadata()
    {
        var logger = new Mock<ILogger<ComicBookInfoProvider>>();
        var path = CreateArchive(ValidComment);
        var provider = new ComicBookInfoProvider(CreateFileSystem(path), logger.Object);

        var result = await provider.ReadMetadata(new ItemInfo(new Book { Path = path }), Mock.Of<IDirectoryService>(), CancellationToken.None);

        Assert.True(result.HasMetadata);
        Assert.NotNull(result.Item);
        Assert.Equal("Episode 36", result.Item.Name);
        Assert.Equal("Jungle Juice", result.Item.SeriesName);
        Assert.Equal(175, result.Item.IndexNumber);
        VerifyNothingLoggedAbove(logger, LogLevel.Debug);
    }

    [Fact]
    public void ReadTitle_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = ComicBookInfoProvider.ReadComicBookMetadata(_comicBookInfoFormat.Metadata);
        Assert.NotNull(actual);
        Assert.Equal("At Midnight, All the Agents", actual.Name);
    }

    [Fact(DisplayName = "Check that the series has no alternative title.")]
    public void ReadAlternativeSeries_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = ComicBookInfoProvider.ReadComicBookMetadata(_comicBookInfoFormat.Metadata);
        Assert.NotNull(actual);
        Assert.Null(actual.OriginalTitle);
    }

    [Fact]
    public void ReadSeries_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = ComicBookInfoProvider.ReadComicBookMetadata(_comicBookInfoFormat.Metadata);
        Assert.NotNull(actual);
        Assert.Equal("Watchmen", actual.SeriesName);
    }

    [Fact(DisplayName = "Check that the issue equals the index number.")]
    public void ReadNumber_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = ComicBookInfoProvider.ReadComicBookMetadata(_comicBookInfoFormat.Metadata);
        Assert.NotNull(actual);
        Assert.Equal(1, actual.IndexNumber);
    }

    [Fact]
    public void ReadSummary_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = ComicBookInfoProvider.ReadComicBookMetadata(_comicBookInfoFormat.Metadata);

        Assert.NotNull(actual);
        Assert.Equal("Tales of the Black Freighter...", actual.Overview);
    }

    [Fact]
    public void ReadProductionYear_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = ComicBookInfoProvider.ReadComicBookMetadata(_comicBookInfoFormat.Metadata);
        Assert.NotNull(actual);
        Assert.Equal(1986, actual.ProductionYear);
    }

    [Fact]
    public void ReadDate_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = ComicBookInfoProvider.ReadComicBookMetadata(_comicBookInfoFormat.Metadata);
        var expected = new DateTime(1986, 9, 1, 0, 0, 0, DateTimeKind.Unspecified);

        Assert.NotNull(actual);
        Assert.Equal(expected, actual.PremiereDate);
    }

    [Fact]
    public void ReadGenres_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = ComicBookInfoProvider.ReadComicBookMetadata(_comicBookInfoFormat.Metadata);
        Assert.NotNull(actual);
        Assert.Single(actual.Genres);
        Assert.Equal("Superhero", actual.Genres[0]);
    }

    [Fact]
    public void ReadPublisher_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = ComicBookInfoProvider.ReadComicBookMetadata(_comicBookInfoFormat.Metadata);
        Assert.NotNull(actual);
        Assert.Single(actual.Studios);
        Assert.Equal("DC Comics", actual.Studios[0]);
    }

    [Fact]
    public void ReadPeopleMetadata_Success()
    {
        var metadataResult = new MetadataResult<Book> { Item = new Book(), HasMetadata = true };

        Assert.NotNull(_comicBookInfoFormat.Metadata);
        ComicBookInfoProvider.ReadPeopleMetadata(_comicBookInfoFormat.Metadata, metadataResult);

        Assert.Collection(
            metadataResult.People,
            person =>
            {
                Assert.Equal("Alan Moore", person.Name);
                Assert.Equal(PersonKind.Writer, person.Type);
            },
            person =>
            {
                Assert.Equal("Dave Gibbons", person.Name);
                Assert.Equal(PersonKind.Artist, person.Type);
            },
            person =>
            {
                Assert.Equal("Dave Gibbons", person.Name);
                Assert.Equal(PersonKind.Letterer, person.Type);
            },
            person =>
            {
                Assert.Equal("John Gibbons", person.Name);
                Assert.Equal(PersonKind.Colorist, person.Type);
            },
            person =>
            {
                Assert.Equal("Len Wein", person.Name);
                Assert.Equal(PersonKind.Editor, person.Type);
            },
            person =>
            {
                Assert.Equal("Barbara Kesel", person.Name);
                Assert.Equal(PersonKind.Editor, person.Type);
            },
            person =>
            {
                Assert.Equal("Takashi Shimoyama", person.Name);
                Assert.Equal(PersonKind.Unknown, person.Type);
            });
    }

    [Fact]
    public void ReadTags_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);

        var actual = ComicBookInfoProvider.ReadComicBookMetadata(_comicBookInfoFormat.Metadata);

        Assert.NotNull(actual);
        Assert.Equal(["Rorschach", "Ozymandias", "Nite Owl"], actual.Tags);
    }

    [Fact]
    public void ReadCultureInfoInto_Success()
    {
        Assert.NotNull(_comicBookInfoFormat.Metadata);
        Assert.NotNull(_comicBookInfoFormat.Metadata.Language);

        // ComicBookInfo stores the language as a display name rather than an ISO code. ICU accepts
        // it as a custom culture, so the name is passed through instead of being mapped to "en".
        var actual = ComicBookInfoProvider.ReadCultureInfoInto(_comicBookInfoFormat.Metadata.Language);

        Assert.Equal("english", actual);
    }

    [Fact]
    public void ReadCultureInfoInto_UnknownLanguage_ReturnsNull()
    {
        Assert.Null(ComicBookInfoProvider.ReadCultureInfoInto("notalanguage"));
    }

    private static void VerifyLogged(Mock<ILogger<ComicBookInfoProvider>> logger, LogLevel level, string message)
    {
        logger.Verify(
            x => x.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(message, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static void VerifyNothingLoggedAbove(Mock<ILogger<ComicBookInfoProvider>> logger, LogLevel level)
    {
        // a comment that holds no ComicBookInfo is normal, so it must not reach the log of a default install
        logger.Verify(
            x => x.Log(
                It.Is<LogLevel>(actual => actual > level),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    private static IFileSystem CreateFileSystem(string path)
    {
        var fileSystem = new Mock<IFileSystem>();
        fileSystem.Setup(x => x.GetFileSystemInfo(path))
            .Returns(new FileSystemMetadata
            {
                Exists = true,
                FullName = path,
                Name = Path.GetFileName(path),
                Extension = ".cbz",
                IsDirectory = false
            });

        return fileSystem.Object;
    }

    private string CreateArchive(string? comment)
    {
        var path = Path.Combine(_directory, Guid.NewGuid().ToString("N") + ".cbz");

        using (var stream = File.Create(path))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            if (comment is not null)
            {
                archive.Comment = comment;
            }

            var entry = archive.CreateEntry("ComicInfo.xml");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("<ComicInfo />");
        }

        return path;
    }

    private static ComicBookInfoFormat GenerateTestData()
    {
        // example data taken from https://code.google.com/archive/p/comicbookinfo/wikis/Example.wiki
        var credits = new[]
        {
            new ComicBookInfoCredit { Person = "Moore, Alan", Role = "Writer" },
            new ComicBookInfoCredit { Person = "Gibbons, Dave", Role = "Artist" },
            new ComicBookInfoCredit { Person = "Gibbons, Dave", Role = "Letterer" },
            new ComicBookInfoCredit { Person = "Gibbons, John", Role = "Colorer" },
            new ComicBookInfoCredit { Person = "Wein, Len", Role = "Editor" },
            new ComicBookInfoCredit { Person = "Kesel, Barbara", Role = "Editor" },
            // example of a non-comma-separated name
            new ComicBookInfoCredit { Person = "Takashi Shimoyama", Role = "Example" }
        };

        var metadata = new ComicBookInfoMetadata
        {
            Series = "Watchmen",
            Title = "At Midnight, All the Agents",
            Publisher = "DC Comics",
            PublicationMonth = 9,
            PublicationYear = 1986,
            Issue = 1,
            NumberOfIssues = 12,
            Volume = 1,
            NumberOfVolumes = 1,
            Rating = 5,
            Genre = "Superhero",
            Language = "English",
            Country = "United States",
            Credits = credits,
            Tags = ["Rorschach", "Ozymandias", "Nite Owl"],
            Comments = "Tales of the Black Freighter...",
        };

        return new ComicBookInfoFormat
        {
            AppId = "ComicBookLover/888",
            LastModified = "2009-10-25 14:51:31 +0000",
            Metadata = metadata
        };
    }
}
