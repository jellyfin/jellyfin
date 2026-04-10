using System.Collections.Generic;
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

    /// <summary>
    /// AudioBookResolver resolves directories containing audio files as AudioBook folders.
    /// </summary>
    [Theory]
    [InlineData("words.mp3")]
    [InlineData("chapter 01.mp3")]
    [InlineData("part 1.mp3")]
    [InlineData("chapter 01.mp3", "non-media.txt")]
    [InlineData("01.mp3", "subdirectory/")]
    [InlineData("01.mp3", "02.mp3")]
    [InlineData("chapter 01.mp3", "chapter 02.mp3")]
    [InlineData("Name.mp3", "Another Name.mp3")]
    [InlineData("familyhappiness_1_tolstoy.mp3", "familyhappiness_2_tolstoy.mp3")]
    [InlineData("cossacks_01_tolstoy.mp3", "cossacks_02_tolstoy.mp3")]
    public void AudioBookResolver_DirectoryWithAudio_ReturnsAudioBook(params string[] children)
    {
        var resolved = ResolveDirectory("/parent/book title", children);
        Assert.NotNull(resolved);
        Assert.IsType<AudioBook>(resolved);
    }

    /// <summary>
    /// AudioBookResolver returns null for directories without audio files.
    /// </summary>
    [Theory]
    [InlineData]
    [InlineData("subdirectory/")]
    [InlineData("non-media.txt")]
    public void AudioBookResolver_DirectoryWithoutAudio_ReturnsNull(params string[] children)
    {
        var resolved = ResolveDirectory("/parent/book title", children);
        Assert.Null(resolved);
    }

    /// <summary>
    /// AudioBookResolver resolves directories whose only audio is inside part subfolders as AudioBook.
    /// </summary>
    [Theory]
    [InlineData("Part 1/", "Part 2/")]
    [InlineData("part 1/", "part 2/")]
    [InlineData("Part 01/", "Part 02/")]
    public void AudioBookResolver_DirectoryWithPartSubfolders_ReturnsAudioBook(params string[] partFolders)
    {
        // Each part subfolder contains at least one audio file.
        var partFolderAudio = partFolders.ToDictionary(
            f => "/parent/book title/" + f.TrimEnd('/'),
            f => new[] { new FileSystemMetadata { FullName = "/parent/book title/" + f.TrimEnd('/') + "/chapter 01.mp3", IsDirectory = false } });

        var resolved = ResolveDirectoryWithSubfolderAudio("/parent/book title", partFolders, partFolderAudio);
        Assert.NotNull(resolved);
        Assert.IsType<AudioBook>(resolved);
    }

    /// <summary>
    /// AudioBookResolver returns null when subfolders exist but are not part folders.
    /// </summary>
    [Theory]
    [InlineData("extras/")]
    [InlineData("bonus/")]
    public void AudioBookResolver_DirectoryWithNonPartSubfolders_ReturnsNull(params string[] subfolders)
    {
        var subfolderAudio = subfolders.ToDictionary(
            f => "/parent/book title/" + f.TrimEnd('/'),
            f => new[] { new FileSystemMetadata { FullName = "/parent/book title/" + f.TrimEnd('/') + "/track.mp3", IsDirectory = false } });

        var resolved = ResolveDirectoryWithSubfolderAudio("/parent/book title", subfolders, subfolderAudio);
        Assert.Null(resolved);
    }

    /// <summary>
    /// AudioResolver resolves individual audio files in a books collection as Audio items.
    /// </summary>
    [Theory]
    [InlineData("track.mp3")]
    [InlineData("chapter 01.mp3")]
    [InlineData("familyhappiness_1_tolstoy.mp3")]
    public void AudioResolver_FileInBooksCollection_ReturnsAudio(string fileName)
    {
        var resolver = new AudioResolver(_namingOptions);
        var itemResolveArgs = new ItemResolveArgs(
            null,
            Mock.Of<ILibraryManager>())
        {
            CollectionType = CollectionType.books,
            FileInfo = new FileSystemMetadata
            {
                FullName = "/parent/book/" + fileName,
                IsDirectory = false
            },
            Path = "/parent/book/" + fileName
        };

        var result = resolver.Resolve(itemResolveArgs);
        Assert.NotNull(result);
        Assert.IsType<Audio>(result);
    }

    /// <summary>
    /// AudioResolver returns null for directories in a books collection (handled by AudioBookResolver).
    /// </summary>
    [Fact]
    public void AudioResolver_DirectoryInBooksCollection_ReturnsNull()
    {
        var resolver = new AudioResolver(_namingOptions);
        var itemResolveArgs = new ItemResolveArgs(
            null,
            Mock.Of<ILibraryManager>())
        {
            CollectionType = CollectionType.books,
            FileInfo = new FileSystemMetadata
            {
                FullName = "/parent/book title",
                IsDirectory = true
            }
        };

        var result = resolver.Resolve(itemResolveArgs);
        Assert.Null(result);
    }

    private static AudioBook? ResolveDirectory(string parent, string[] children)
    {
        var childrenMetadata = children.Select(name => new FileSystemMetadata
        {
            FullName = parent + "/" + name,
            IsDirectory = name.EndsWith('/')
        }).ToArray();

        var directoryService = Mock.Of<IDirectoryService>();
        var parentFolder = new Folder { Path = "/parent" };
        var resolver = new AudioBookResolver(_namingOptions, directoryService);
        var itemResolveArgs = new ItemResolveArgs(
            null,
            Mock.Of<ILibraryManager>())
        {
            CollectionType = CollectionType.books,
            Parent = parentFolder,
            FileInfo = new FileSystemMetadata
            {
                FullName = parent,
                IsDirectory = true
            },
            FileSystemChildren = childrenMetadata
        };

        return resolver.Resolve(itemResolveArgs);
    }

    private static AudioBook? ResolveDirectoryWithSubfolderAudio(
        string parent,
        string[] subfolders,
        Dictionary<string, FileSystemMetadata[]> subfolderContents)
    {
        var childrenMetadata = subfolders.Select(name => new FileSystemMetadata
        {
            FullName = parent + "/" + name.TrimEnd('/'),
            IsDirectory = true
        }).ToArray();

        var directoryServiceMock = new Mock<IDirectoryService>();
        foreach (var (path, contents) in subfolderContents)
        {
            directoryServiceMock.Setup(d => d.GetFileSystemEntries(path)).Returns(contents);
        }

        var parentFolder = new Folder { Path = "/parent" };
        var resolver = new AudioBookResolver(_namingOptions, directoryServiceMock.Object);
        var itemResolveArgs = new ItemResolveArgs(
            null,
            Mock.Of<ILibraryManager>())
        {
            CollectionType = CollectionType.books,
            Parent = parentFolder,
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
