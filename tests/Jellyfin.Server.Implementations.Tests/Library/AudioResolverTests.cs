using System.Linq;
using Emby.Naming.Common;
using Emby.Server.Implementations.Library.Resolvers.Audio;
using Jellyfin.Data.Enums;
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
    /* Results that are an audio book but not currently navigable as such (multiple chapters and/or parts). */
    [InlineData("01.mp3", "02.mp3")]
    [InlineData("chapter 01.mp3", "chapter 02.mp3")]
    [InlineData("part 1.mp3", "part 2.mp3")]
    [InlineData("chapter 01 part 01.mp3", "chapter 01 part 02.mp3")]
    /* Mismatched chapters, parts, and named files. */
    [InlineData("chapter 01.mp3", "part 2.mp3")]
    [InlineData("book title.mp3", "chapter name.mp3")] // "book title" resolves as alternate version of book based on directory name
    [InlineData("01 Content.mp3", "01 Credits.mp3")] // resolves as alternate versions of chapter 1
    [InlineData("Chapter Name.mp3", "Part 1.mp3")]
    public void Resolve_AudiobookDirectory_NoResult(params string[] children)
    {
        var resolved = TestResolveChildren("/parent/book title", children);
        Assert.Null(resolved);
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
