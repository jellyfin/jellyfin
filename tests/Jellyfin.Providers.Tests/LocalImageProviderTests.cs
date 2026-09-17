using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.LocalMetadata.Images;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests;

public sealed class LocalImageProviderTests : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory();

    [Theory]
    [InlineData(".hidden.jpg")]
    [InlineData("._metadata.jpg")]
    public void GetImages_ExtraFanartHiddenFile_IsExcluded(string name)
    {
        var images = GetImages(new[] { name });

        Assert.Empty(images);
    }

    [Theory]
    [InlineData("landscape.jpg")]
    [InlineData("scene.with.dots.jpg")]
    [InlineData("_metadata.jpg")]
    public void GetImages_ExtraFanartVisibleFile_IsBackdrop(string name)
    {
        var image = Assert.Single(GetImages(new[] { name }));

        Assert.Equal(ImageType.Backdrop, image.Type);
        Assert.Equal(Path.Combine(_directory.FullName, "extrafanart", name), image.FileInfo.FullName);
    }

    [Fact]
    public void GetImages_ExtraFanartMixedFiles_KeepsOnlyVisibleBackdrop()
    {
        var image = Assert.Single(GetImages(new[] { ".hidden.jpg", "scene.jpg", "._metadata.jpg" }));

        Assert.Equal(ImageType.Backdrop, image.Type);
        Assert.Equal("scene.jpg", image.FileInfo.Name);
    }

    [Fact]
    public void GetImages_ExtraFanartEmpty_ReturnsNoImages()
    {
        Assert.Empty(GetImages(Array.Empty<string>()));
    }

    [Theory]
    [InlineData("backdrop.jpg", ImageType.Backdrop)]
    [InlineData("poster.jpg", ImageType.Primary)]
    [InlineData("logo.png", ImageType.Logo)]
    public void GetImages_NamedArtworkOutsideExtraFanart_IsPreserved(string name, ImageType type)
    {
        var image = Assert.Single(GetImages(Array.Empty<string>(), name));

        Assert.Equal(type, image.Type);
        Assert.Equal(Path.Combine(_directory.FullName, name), image.FileInfo.FullName);
    }

    public void Dispose()
    {
        _directory.Delete(true);
    }

    private LocalImageInfo[] GetImages(string[] extraNames, string? namedArtwork = null)
    {
        var extraPath = Path.Combine(_directory.FullName, "extrafanart");
        var entries = new List<FileSystemMetadata>
        {
            new() { Name = "extrafanart", FullName = extraPath, IsDirectory = true }
        };
        if (namedArtwork is not null)
        {
            entries.Add(ImageFile(_directory.FullName, namedArtwork));
        }

        var directoryService = new Mock<IDirectoryService>();
        directoryService.Setup(service => service.GetFileSystemEntries(_directory.FullName)).Returns(entries.ToArray());
        var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
        fileSystem.Setup(fs => fs.GetFiles(extraPath, It.IsAny<IReadOnlyList<string>>(), false, false))
            .Returns(extraNames.Select(name => ImageFile(extraPath, name)));
        var provider = new LocalImageProvider(fileSystem.Object);
        var movie = new Movie { Path = Path.Combine(_directory.FullName, "movie.mkv") };
        BaseItem.MediaSourceManager = Mock.Of<IMediaSourceManager>();

        return provider.GetImages(movie, directoryService.Object).ToArray();
    }

    private static FileSystemMetadata ImageFile(string directory, string name)
    {
        return new FileSystemMetadata
        {
            Name = name,
            FullName = Path.Combine(directory, name),
            Extension = Path.GetExtension(name),
            Length = 1,
            Exists = true
        };
    }
}
