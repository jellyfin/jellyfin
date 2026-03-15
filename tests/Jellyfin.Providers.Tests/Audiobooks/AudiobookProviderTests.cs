using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Audiobooks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.Audiobooks;

public class AudiobookProviderTests
{
    private readonly AudiobookProvider _provider;
    private readonly Mock<IFileSystem> _fileSystemMock;
    private readonly Mock<ILogger<AudiobookProvider>> _loggerMock;

    public AudiobookProviderTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
        _loggerMock = new Mock<ILogger<AudiobookProvider>>();
        _provider = new AudiobookProvider(_fileSystemMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        // Assert
        Assert.Equal("Audiobook Metadata", _provider.Name);
    }

    [Theory]
    [InlineData("/audiobooks/book.m4b")]
    [InlineData("/audiobooks/book.mp3")]
    [InlineData("/audiobooks/book.m4a")]
    [InlineData("/audiobooks/book.aac")]
    [InlineData("/audiobooks/book.ogg")]
    [InlineData("/audiobooks/book.flac")]
    [InlineData("/audiobooks/book.wma")]
    public async Task GetMetadata_SupportedExtension_ProcessesFile(string path)
    {
        // Arrange
        var audiobook = new AudioBook { Path = path };
        var itemInfo = new ItemInfo(audiobook);
        var fileMetadata = new FileSystemMetadata
        {
            FullName = path,
            IsDirectory = false
        };

        _fileSystemMock.Setup(fs => fs.GetFileSystemInfo(path))
            .Returns(fileMetadata);

        // Act
        var result = await _provider.GetMetadata(itemInfo, Mock.Of<IDirectoryService>(), default);

        // Assert
        // We can't test the actual metadata extraction without real audio files,
        // but we can verify the file was recognized and processed
        _fileSystemMock.Verify(fs => fs.GetFileSystemInfo(path), Times.Once);
    }

    [Theory]
    [InlineData("/audiobooks/book.txt")]
    [InlineData("/audiobooks/book.pdf")]
    [InlineData("/audiobooks/book.epub")]
    [InlineData("/audiobooks/book.mp4")]
    public async Task GetMetadata_UnsupportedExtension_ReturnsNoMetadata(string path)
    {
        // Arrange
        var audiobook = new AudioBook { Path = path };
        var itemInfo = new ItemInfo(audiobook);
        var fileMetadata = new FileSystemMetadata
        {
            FullName = path,
            IsDirectory = false
        };

        _fileSystemMock.Setup(fs => fs.GetFileSystemInfo(path))
            .Returns(fileMetadata);

        // Act
        var result = await _provider.GetMetadata(itemInfo, Mock.Of<IDirectoryService>(), default);

        // Assert
        Assert.False(result.HasMetadata);
    }

    [Fact]
    public async Task GetMetadata_Directory_ReturnsNoMetadata()
    {
        // Arrange
        var path = "/audiobooks/book_folder";
        var audiobook = new AudioBook { Path = path };
        var itemInfo = new ItemInfo(audiobook);
        var fileMetadata = new FileSystemMetadata
        {
            FullName = path,
            IsDirectory = true
        };

        _fileSystemMock.Setup(fs => fs.GetFileSystemInfo(path))
            .Returns(fileMetadata);

        // Act
        var result = await _provider.GetMetadata(itemInfo, Mock.Of<IDirectoryService>(), default);

        // Assert
        Assert.False(result.HasMetadata);
    }

    [Fact]
    public async Task GetMetadata_EmptyPath_ReturnsNoMetadata()
    {
        // Arrange
        var audiobook = new AudioBook { Path = string.Empty };
        var itemInfo = new ItemInfo(audiobook);
        var fileMetadata = new FileSystemMetadata
        {
            FullName = string.Empty,
            IsDirectory = false
        };

        _fileSystemMock.Setup(fs => fs.GetFileSystemInfo(string.Empty))
            .Returns(fileMetadata);

        // Act
        var result = await _provider.GetMetadata(itemInfo, Mock.Of<IDirectoryService>(), default);

        // Assert
        Assert.False(result.HasMetadata);
    }

    [Fact]
    public async Task GetMetadata_NullFileSystemInfo_ReturnsNoMetadata()
    {
        // Arrange
        var path = "/audiobooks/book.m4b";
        var audiobook = new AudioBook { Path = path };
        var itemInfo = new ItemInfo(audiobook);

        _fileSystemMock.Setup(fs => fs.GetFileSystemInfo(path))
            .Returns((FileSystemMetadata)null!);

        // Act
        var result = await _provider.GetMetadata(itemInfo, Mock.Of<IDirectoryService>(), default);

        // Assert
        Assert.False(result.HasMetadata);
    }
}
