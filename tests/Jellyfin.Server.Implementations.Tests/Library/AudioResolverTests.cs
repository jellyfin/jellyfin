using System.Linq;
using Emby.Naming.Common;
using Emby.Server.Implementations.Library.Resolvers.Audio;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library;

public class AudioResolverTests
{
    private static readonly NamingOptions _namingOptions = new();

    [Theory]
    [InlineData("words.mp3")] // single non-tagged file
    [InlineData("chapter 01.mp3")]
    [InlineData("part 1.mp3")]
    [InlineData("chapter 01.mp3", "non-media.txt")]
    [InlineData("title.mp3", "title.epub")]
    [InlineData("01.mp3", "subdirectory/")] // single media file with sub-directory - note that this will hide any contents in the subdirectory
    /* Multi-part books stack into one item when every file has a distinct order number. */
    [InlineData("01.mp3", "02.mp3")]
    [InlineData("chapter 01.mp3", "chapter 02.mp3")]
    [InlineData("part 1.mp3", "part 2.mp3")]
    [InlineData("chapter 01 part 01.mp3", "chapter 01 part 02.mp3")]
    [InlineData("chapter 01.mp3", "part 2.mp3")]
    /* Descriptive names stack as long as the leading order number is distinct, even when the
       descriptive text contains its own numbers that would otherwise collide. */
    [InlineData("01 Opening Credits.mp3", "02 Prologue.mp3", "03 Part I.mp3", "04 Chapter 1.mp3", "05 Chapter 2.mp3")]
    [InlineData("Defiant Part 1 01.mp3", "Defiant Chapter 1 02.mp3", "Defiant Chapter 2 03.mp3")]
    public void Resolve_AudiobookDirectory_SingleResult(params string[] children)
    {
        var resolved = TestResolveChildren("/parent/title", children);
        Assert.NotNull(resolved);
    }

    [Theory]
    /* Results that can't be displayed as an audio book. */
    [InlineData] // no contents
    [InlineData("subdirectory/")]
    [InlineData("non-media.txt")]
    /* Names don't indicate parts of a single book. */
    [InlineData("Name.mp3", "Another Name.mp3")]
    /* Same order number, so they are alternate versions rather than separate parts. */
    [InlineData("01 Content.mp3", "01 Credits.mp3")]
    [InlineData("book title.mp3", "chapter name.mp3")] // "book title" resolves as alternate version of book based on directory name
    /* A file without an order number breaks the sequence. */
    [InlineData("Chapter Name.mp3", "Part 1.mp3")]
    public void Resolve_AudiobookDirectory_NoResult(params string[] children)
    {
        var resolved = TestResolveChildren("/parent/book title", children);
        Assert.Null(resolved);
    }

    [Fact]
    public void Resolve_TrackNumberedParts_StackInOrder()
    {
        var resolved = TestResolveChildren(
            "/parent/Defiant",
            ["03 Chapter 2.mp3", "01 Opening Credits.mp3", "02 Chapter 1.mp3"]);

        var book = Assert.IsType<AudioBook>(resolved);
        Assert.Equal("/parent/Defiant/01 Opening Credits.mp3", book.Path);
        Assert.Equal(
            ["/parent/Defiant/02 Chapter 1.mp3", "/parent/Defiant/03 Chapter 2.mp3"],
            book.AdditionalParts);
    }

    private Audio? TestResolveChildren(string parent, string[] children)
    {
        var childrenMetadata = children.Select(name => new FileSystemMetadata
        {
            FullName = parent + "/" + name,
            IsDirectory = name.EndsWith('/')
        }).ToArray();

        var resolver = new AudioResolver(_namingOptions);
        var itemResolveArgs = new ItemResolveArgs(
            null,
            Mock.Of<ILibraryManager>())
        {
            CollectionType = CollectionType.books,
            FileInfo = new FileSystemMetadata
            {
                FullName = parent,
                IsDirectory = true
            },
            FileSystemChildren = childrenMetadata
        };

        return resolver.Resolve(itemResolveArgs);
    }
}
