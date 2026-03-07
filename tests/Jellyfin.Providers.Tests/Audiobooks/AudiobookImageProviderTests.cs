using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Audiobooks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.Audiobooks;

public class AudiobookImageProviderTests
{
    private readonly AudiobookImageProvider _provider;
    private readonly Mock<ILogger<AudiobookImageProvider>> _loggerMock;

    public AudiobookImageProviderTests()
    {
        _loggerMock = new Mock<ILogger<AudiobookImageProvider>>();
        _provider = new AudiobookImageProvider(_loggerMock.Object);
    }

    [Fact]
    public void Name_ReturnsCorrectValue()
    {
        // Assert
        Assert.Equal("Audiobook Metadata", _provider.Name);
    }

    [Fact]
    public void Supports_AudioBookWithValidExtension_ReturnsTrue()
    {
        // Arrange
        var audiobook = new AudioBook
        {
            Path = "/audiobooks/book.m4b"
        };

        // Act
        var result = _provider.Supports(audiobook);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("/audiobooks/book.m4b")]
    [InlineData("/audiobooks/book.mp3")]
    [InlineData("/audiobooks/book.m4a")]
    [InlineData("/audiobooks/book.aac")]
    [InlineData("/audiobooks/book.ogg")]
    [InlineData("/audiobooks/book.flac")]
    [InlineData("/audiobooks/book.wma")]
    public void Supports_AudioBookWithSupportedExtensions_ReturnsTrue(string path)
    {
        // Arrange
        var audiobook = new AudioBook
        {
            Path = path
        };

        // Act
        var result = _provider.Supports(audiobook);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("/audiobooks/book.txt")]
    [InlineData("/audiobooks/book.pdf")]
    [InlineData("/audiobooks/book.epub")]
    [InlineData("/audiobooks/book.mp4")]
    public void Supports_AudioBookWithUnsupportedExtension_ReturnsFalse(string path)
    {
        // Arrange
        var audiobook = new AudioBook
        {
            Path = path
        };

        // Act
        var result = _provider.Supports(audiobook);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Supports_NotAudioBook_ReturnsFalse()
    {
        // Arrange
        var book = new Book
        {
            Path = "/books/book.epub"
        };

        // Act
        var result = _provider.Supports(book);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Supports_AudioBookWithEmptyPath_ReturnsFalse()
    {
        // Arrange
        var audiobook = new AudioBook
        {
            Path = string.Empty
        };

        // Act
        var result = _provider.Supports(audiobook);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Supports_AudioBookWithNullPath_ReturnsFalse()
    {
        // Arrange
        var audiobook = new AudioBook
        {
            Path = null!
        };

        // Act
        var result = _provider.Supports(audiobook);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetSupportedImages_ReturnsOnlyPrimary()
    {
        // Arrange
        var audiobook = new AudioBook
        {
            Path = "/audiobooks/book.m4b"
        };

        // Act
        var result = _provider.GetSupportedImages(audiobook);

        // Assert
        Assert.Single(result);
        Assert.Contains(ImageType.Primary, result);
    }

    [Fact]
    public async Task GetImage_NonPrimaryImageType_ReturnsNoImage()
    {
        // Arrange
        var audiobook = new AudioBook
        {
            Path = "/audiobooks/book.m4b"
        };

        // Act
        var result = await _provider.GetImage(audiobook, ImageType.Backdrop, default);

        // Assert
        Assert.False(result.HasImage);
    }

    [Fact]
    public async Task GetImage_InvalidFile_ReturnsNoImage()
    {
        // Arrange
        var audiobook = new AudioBook
        {
            Path = "/nonexistent/book.m4b"
        };

        // Act
        var result = await _provider.GetImage(audiobook, ImageType.Primary, default);

        // Assert
        Assert.False(result.HasImage);
    }
}
