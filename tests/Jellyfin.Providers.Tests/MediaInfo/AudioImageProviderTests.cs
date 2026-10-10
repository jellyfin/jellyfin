using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo
{
    [Collection("LibraryManagerTests")]
    public sealed class AudioImageProviderTests : IDisposable
    {
        private readonly DirectoryInfo _cacheDirectory = Directory.CreateTempSubdirectory();
        private readonly Mock<ILibraryManager> _libraryManager = new(MockBehavior.Strict);
        private readonly ILibraryManager _previousLibraryManager;
        private readonly LibraryOptions _libraryOptions = new();

        public AudioImageProviderTests()
        {
            _previousLibraryManager = BaseItem.LibraryManager;
            BaseItem.LibraryManager = _libraryManager.Object;
            _libraryManager.Setup(manager => manager.GetLibraryOptions(It.Is<BaseItem>(item => item.GetType() == typeof(Audio))))
                .Returns(_libraryOptions);
        }

        [Fact]
        public async Task GetImage_SameNamedAlbums_ReturnsDistinctImages()
        {
            _libraryOptions.EnableAlbumSpecificImageExtraction = true;
            var firstAlbum = new MusicAlbum { Id = Guid.NewGuid() };
            var secondAlbum = new MusicAlbum { Id = Guid.NewGuid() };
            _libraryManager.Setup(manager => manager.GetItemById(firstAlbum.Id)).Returns(firstAlbum);
            _libraryManager.Setup(manager => manager.GetItemById(secondAlbum.Id)).Returns(secondAlbum);

            var firstTrack = CreateTrack(firstAlbum.Id);
            var secondTrack = CreateTrack(secondAlbum.Id);
            var provider = CreateProvider(firstTrack, secondTrack);

            var firstImage = await provider.GetImage(firstTrack, ImageType.Primary, TestContext.Current.CancellationToken);
            var secondImage = await provider.GetImage(secondTrack, ImageType.Primary, TestContext.Current.CancellationToken);

            Assert.True(firstImage.HasImage);
            Assert.True(secondImage.HasImage);
            Assert.NotEqual(firstImage.Path, secondImage.Path);
            Assert.Equal(firstTrack.Path, await File.ReadAllTextAsync(firstImage.Path, TestContext.Current.CancellationToken));
            Assert.Equal(secondTrack.Path, await File.ReadAllTextAsync(secondImage.Path, TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData(1997, 1999)]
        [InlineData(1997, null)]
        [InlineData(null, null)]
        public async Task GetImage_SameAlbumWithDifferentOrMissingDates_ReusesImage(int? firstYear, int? secondYear)
        {
            _libraryOptions.EnableAlbumSpecificImageExtraction = true;
            var album = new MusicAlbum { Id = Guid.NewGuid() };
            _libraryManager.Setup(manager => manager.GetItemById(album.Id)).Returns(album);
            var firstTrack = CreateTrack(album.Id);
            var secondTrack = CreateTrack(album.Id);
            firstTrack.PremiereDate = firstYear.HasValue ? new DateTime(firstYear.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc) : null;
            secondTrack.PremiereDate = secondYear.HasValue ? new DateTime(secondYear.Value, 1, 1, 0, 0, 0, DateTimeKind.Utc) : null;
            secondTrack.Album = null;
            secondTrack.AlbumArtists = Array.Empty<string>();
            var provider = CreateProvider(firstTrack, secondTrack);

            var firstImage = await provider.GetImage(firstTrack, ImageType.Primary, TestContext.Current.CancellationToken);
            var secondImage = await provider.GetImage(secondTrack, ImageType.Primary, TestContext.Current.CancellationToken);
            var cachedImage = await provider.GetImage(firstTrack, ImageType.Primary, TestContext.Current.CancellationToken);

            Assert.Equal(album.Id.ToString("N", CultureInfo.InvariantCulture) + ".jpg", Path.GetFileName(firstImage.Path));
            Assert.Equal(firstImage.Path, secondImage.Path);
            Assert.Equal(firstImage.Path, cachedImage.Path);
            Assert.Equal(firstTrack.Path, await File.ReadAllTextAsync(secondImage.Path, TestContext.Current.CancellationToken));
            Assert.Single(Directory.GetFiles(_cacheDirectory.FullName, "*.jpg", SearchOption.AllDirectories));
        }

        [Fact]
        public async Task GetImage_DiscSubfolders_ReusesAlbumImage()
        {
            _libraryOptions.EnableAlbumSpecificImageExtraction = true;
            var album = new MusicAlbum { Id = Guid.NewGuid() };
            var firstDisc = new Folder { Id = Guid.NewGuid(), ParentId = album.Id };
            var secondDisc = new Folder { Id = Guid.NewGuid(), ParentId = album.Id };
            _libraryManager.Setup(manager => manager.GetItemById(album.Id)).Returns(album);
            _libraryManager.Setup(manager => manager.GetItemById(firstDisc.Id)).Returns(firstDisc);
            _libraryManager.Setup(manager => manager.GetItemById(secondDisc.Id)).Returns(secondDisc);
            var firstTrack = CreateTrack(firstDisc.Id);
            var secondTrack = CreateTrack(secondDisc.Id);
            var provider = CreateProvider(firstTrack, secondTrack);

            var firstImage = await provider.GetImage(firstTrack, ImageType.Primary, TestContext.Current.CancellationToken);
            var secondImage = await provider.GetImage(secondTrack, ImageType.Primary, TestContext.Current.CancellationToken);

            Assert.Equal(album.Id.ToString("N", CultureInfo.InvariantCulture) + ".jpg", Path.GetFileName(firstImage.Path));
            Assert.Equal(firstImage.Path, secondImage.Path);
            Assert.Equal(firstTrack.Path, await File.ReadAllTextAsync(secondImage.Path, TestContext.Current.CancellationToken));
            Assert.Single(Directory.GetFiles(_cacheDirectory.FullName, "*.jpg", SearchOption.AllDirectories));
        }

        [Fact]
        public async Task GetImage_DefaultOptions_ReusesLegacyImageWithoutAlbumLookup()
        {
            Assert.False(_libraryOptions.EnableAlbumSpecificImageExtraction);
            var firstTrack = CreateTrack(Guid.NewGuid());
            var secondTrack = CreateTrack(Guid.NewGuid());
            var provider = CreateProvider(firstTrack, secondTrack);
            var filename = (firstTrack.Album + "-" + firstTrack.AlbumArtists[0]).GetMD5().ToString("N", CultureInfo.InvariantCulture) + ".jpg";

            var firstImage = await provider.GetImage(firstTrack, ImageType.Primary, TestContext.Current.CancellationToken);
            var secondImage = await provider.GetImage(secondTrack, ImageType.Primary, TestContext.Current.CancellationToken);

            Assert.Equal(Path.Combine(_cacheDirectory.FullName, "extracted-audio-images", filename[..1], filename), firstImage.Path);
            Assert.Equal(firstImage.Path, secondImage.Path);
            Assert.Equal(firstTrack.Path, await File.ReadAllTextAsync(secondImage.Path, TestContext.Current.CancellationToken));
            Assert.Single(Directory.GetFiles(_cacheDirectory.FullName, "*.jpg", SearchOption.AllDirectories));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetImage_NoAlbum_ReturnsItemSpecificImages(bool enabled)
        {
            _libraryOptions.EnableAlbumSpecificImageExtraction = enabled;
            var folder = new Folder { Id = Guid.NewGuid() };
            _libraryManager.Setup(manager => manager.GetItemById(folder.Id)).Returns(folder);
            var firstTrack = CreateTrack(folder.Id);
            var secondTrack = CreateTrack(folder.Id);
            firstTrack.Album = null;
            firstTrack.AlbumArtists = Array.Empty<string>();
            secondTrack.Album = null;
            secondTrack.AlbumArtists = Array.Empty<string>();
            var provider = CreateProvider(firstTrack, secondTrack);

            var firstImage = await provider.GetImage(firstTrack, ImageType.Primary, TestContext.Current.CancellationToken);
            var secondImage = await provider.GetImage(secondTrack, ImageType.Primary, TestContext.Current.CancellationToken);

            Assert.Equal(firstTrack.Id.ToString("N", CultureInfo.InvariantCulture) + ".jpg", Path.GetFileName(firstImage.Path));
            Assert.Equal(secondTrack.Id.ToString("N", CultureInfo.InvariantCulture) + ".jpg", Path.GetFileName(secondImage.Path));
            Assert.Equal(firstTrack.Path, await File.ReadAllTextAsync(firstImage.Path, TestContext.Current.CancellationToken));
            Assert.Equal(secondTrack.Path, await File.ReadAllTextAsync(secondImage.Path, TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GetImage_AudioBooks_ReturnsItemSpecificImagesWithoutLibraryLookup(bool enabled)
        {
            _libraryOptions.EnableAlbumSpecificImageExtraction = enabled;
            var firstBook = new AudioBook
            {
                Id = Guid.NewGuid(),
                Path = "first.mp3",
                Album = "Same book",
                AlbumArtists = new[] { "Same author" }
            };
            var secondBook = new AudioBook
            {
                Id = Guid.NewGuid(),
                Path = "second.mp3",
                Album = firstBook.Album,
                AlbumArtists = firstBook.AlbumArtists
            };
            var provider = CreateProvider(firstBook, secondBook);

            var firstImage = await provider.GetImage(firstBook, ImageType.Primary, TestContext.Current.CancellationToken);
            var secondImage = await provider.GetImage(secondBook, ImageType.Primary, TestContext.Current.CancellationToken);

            Assert.Equal(firstBook.Id.ToString("N", CultureInfo.InvariantCulture) + ".jpg", Path.GetFileName(firstImage.Path));
            Assert.Equal(secondBook.Id.ToString("N", CultureInfo.InvariantCulture) + ".jpg", Path.GetFileName(secondImage.Path));
            Assert.Equal(firstBook.Path, await File.ReadAllTextAsync(firstImage.Path, TestContext.Current.CancellationToken));
            Assert.Equal(secondBook.Path, await File.ReadAllTextAsync(secondImage.Path, TestContext.Current.CancellationToken));
        }

        public void Dispose()
        {
            BaseItem.LibraryManager = _previousLibraryManager;
            _cacheDirectory.Delete(true);
            GC.SuppressFinalize(this);
        }

        private static Audio CreateTrack(Guid parentId)
        {
            var id = Guid.NewGuid();
            return new Audio
            {
                Id = id,
                ParentId = parentId,
                Path = Path.Combine("music", id.ToString("N", CultureInfo.InvariantCulture) + ".mp3"),
                Album = "Days of the New",
                AlbumArtists = new[] { "Days of the New" }
            };
        }

        private AudioImageProvider CreateProvider(params Audio[] tracks)
        {
            var mediaSourceManager = new Mock<IMediaSourceManager>(MockBehavior.Strict);
            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            foreach (var track in tracks)
            {
                mediaSourceManager.Setup(manager => manager.GetMediaStreams(It.Is<MediaStreamQuery>(query => query.ItemId.Equals(track.Id) && query.Type == MediaStreamType.EmbeddedImage)))
                    .Returns(new List<MediaStream> { new() { Type = MediaStreamType.EmbeddedImage, Index = 1, Comment = "front cover" } });
                mediaEncoder.Setup(encoder => encoder.ExtractAudioImage(track.Path, 1, TestContext.Current.CancellationToken))
                    .ReturnsAsync(() =>
                    {
                        var imagePath = Path.Combine(_cacheDirectory.FullName, track.Id.ToString("N", CultureInfo.InvariantCulture) + ".jpg");
                        File.WriteAllText(imagePath, track.Path);
                        return imagePath;
                    });
            }

            var configuration = new Mock<IServerConfigurationManager>();
            configuration.Setup(manager => manager.ApplicationPaths.CachePath).Returns(_cacheDirectory.FullName);
            var fileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            fileSystem.Setup(system => system.DeleteFile(It.IsAny<string>())).Callback<string>(File.Delete);

            return new AudioImageProvider(mediaSourceManager.Object, mediaEncoder.Object, configuration.Object, fileSystem.Object, _libraryManager.Object);
        }
    }
}
