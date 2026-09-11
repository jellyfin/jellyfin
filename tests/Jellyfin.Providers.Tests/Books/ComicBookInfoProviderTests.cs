using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Books.ComicBookInfo;
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

    public ComicBookInfoProviderTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "jf-cbz-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, true);
    }

    [Theory]
    [InlineData(null)] // archive written without ever touching Comment
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Created by some packer")] // a comment that was never ComicBookInfo
    public async Task ReadMetadata_NoComicBookInfoComment_NoMetadataAndNoErrorLogged(string? comment)
    {
        var logger = new Mock<ILogger<ComicBookInfoProvider>>();
        var path = CreateArchive(comment);
        var provider = new ComicBookInfoProvider(CreateFileSystem(path), logger.Object);

        var result = await provider.ReadMetadata(new ItemInfo(new Book { Path = path }), Mock.Of<IDirectoryService>(), CancellationToken.None);

        Assert.False(result.HasMetadata);
        VerifyNoErrorLogged(logger);
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
        VerifyNoErrorLogged(logger);
    }

    private static void VerifyNoErrorLogged(Mock<ILogger<ComicBookInfoProvider>> logger)
    {
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
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
}
