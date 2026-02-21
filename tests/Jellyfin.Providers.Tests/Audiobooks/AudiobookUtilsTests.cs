using MediaBrowser.Providers.Audiobooks;
using Xunit;

namespace Jellyfin.Providers.Tests.Audiobooks;

public class AudiobookUtilsTests
{
    [Theory]
    [InlineData("The Hobbit #1", "The Hobbit", 1)]
    [InlineData("Foundation - Book 1", "Foundation", 1)]
    [InlineData("Dune, Book 2", "Dune", 2)]
    [InlineData("Harry Potter: Book 3", "Harry Potter", 3)]
    [InlineData("Game of Thrones (Book 4)", "Game of Thrones", 4)]
    [InlineData("The Expanse [Book 5]", "The Expanse", 5)]
    [InlineData("Mistborn (1)", "Mistborn", 1)]
    [InlineData("The Wheel of Time [1]", "The Wheel of Time", 1)]
    [InlineData("Dresden Files, Volume 7", "Dresden Files", 7)]
    [InlineData("Stormlight Archive - Vol 2", "Stormlight Archive", 2)]
    [InlineData("Ringworld #2", "Ringworld", 2)]
    [InlineData("Foundation 3", "Foundation", 3)]
    public void TryParseSeriesInfo_ValidPatterns_ReturnsTrue(string title, string expectedSeriesName, int expectedBookNumber)
    {
        // Act
        var result = AudiobookUtils.TryParseSeriesInfo(title, out var seriesName, out var bookNumber);

        // Assert
        Assert.True(result);
        Assert.Equal(expectedSeriesName, seriesName);
        Assert.Equal(expectedBookNumber, bookNumber);
    }

    [Theory]
    [InlineData("The Hobbit")]
    [InlineData("Just a Title")]
    [InlineData("No Numbers Here")]
    [InlineData("")]
    public void TryParseSeriesInfo_NoSeriesInfo_ReturnsFalse(string title)
    {
        // Act
        var result = AudiobookUtils.TryParseSeriesInfo(title, out var seriesName, out var bookNumber);

        // Assert
        Assert.False(result);
        Assert.Equal(title, seriesName);
        Assert.Null(bookNumber);
    }

    [Theory]
    [InlineData("The Hobbit [Unabridged]", "The Hobbit")]
    [InlineData("Dune (Audiobook)", "Dune")]
    [InlineData("Foundation - Unabridged.mp3", "Foundation Unabridged")] // Extension is removed, then hyphens/spaces are normalized
    [InlineData("Harry Potter (ABRIDGED)", "Harry Potter")]
    [InlineData("Game of Thrones [MP3]", "Game of Thrones")]
    [InlineData("The Expanse  -  Multiple  Spaces", "The Expanse Multiple Spaces")]
    public void CleanBookTitle_VariousPatterns_CleansCorrectly(string input, string expected)
    {
        // Act
        var result = AudiobookUtils.CleanBookTitle(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "Unknown Title")]
    [InlineData("", "Unknown Title")]
    public void CleanBookTitle_EmptyOrNull_ReturnsUnknownTitle(string? input, string expected)
    {
        // Act
        var result = AudiobookUtils.CleanBookTitle(input!);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ISBN: 1234567890", "1234567890")]
    [InlineData("ISBN: 1-234-56789-X", "123456789X")]
    [InlineData("Contains ISBN: 1234567890 in text", "1234567890")]
    [InlineData("isbn 1234567890", "1234567890")]
    [InlineData("ISBN 0-123-45678-9", "0123456789")]
    public void ExtractIsbn_ValidIsbn_ReturnsIsbn(string text, string expectedIsbn)
    {
        // Act
        var result = AudiobookUtils.ExtractIsbn(text);

        // Assert
        Assert.Equal(expectedIsbn, result);
    }

    [Theory]
    [InlineData("No ISBN here")]
    [InlineData("ISBN: invalid")]
    [InlineData("")]
    [InlineData(null)]
    public void ExtractIsbn_NoIsbn_ReturnsNull(string? text)
    {
        // Act
        var result = AudiobookUtils.ExtractIsbn(text!);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("/audiobooks/Author Name/Book Title/chapter1.m4b", "Book Title", true)]
    [InlineData("/audiobooks/J.K. Rowling/Harry Potter/book.mp3", "Harry Potter", true)]
    [InlineData("/media/Audiobooks/Stephen King/The Stand/audio.m4b", "The Stand", true)]
    public void TryParseBookTitleFromPath_ValidPath_ReturnsTitle(string path, string expectedTitle, bool expectedResult)
    {
        // Act
        var result = AudiobookUtils.TryParseBookTitleFromPath(path, out var bookTitle);

        // Assert
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedTitle, bookTitle);
    }

    [Theory]
    [InlineData("/audiobooks/book.m4b", false)] // Not enough path segments
    [InlineData("", false)] // Empty path
    [InlineData(null, false)] // Null path
    public void TryParseBookTitleFromPath_InvalidPath_ReturnsFalse(string? path, bool expectedResult)
    {
        // Act
        var result = AudiobookUtils.TryParseBookTitleFromPath(path!, out var bookTitle);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(".m4b")]
    [InlineData(".mp3")]
    [InlineData(".m4a")]
    [InlineData(".aac")]
    [InlineData(".ogg")]
    [InlineData(".flac")]
    [InlineData(".wma")]
    public void SupportedExtensions_ContainsCommonFormats(string extension)
    {
        // Assert
        Assert.Contains(extension, AudiobookUtils.SupportedExtensions);
    }

    [Theory]
    [InlineData(".mp4")]
    [InlineData(".avi")]
    [InlineData(".txt")]
    [InlineData(".pdf")]
    public void SupportedExtensions_DoesNotContainNonAudioFormats(string extension)
    {
        // Assert
        Assert.DoesNotContain(extension, AudiobookUtils.SupportedExtensions);
    }
}
