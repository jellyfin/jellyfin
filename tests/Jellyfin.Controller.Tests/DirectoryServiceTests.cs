using System.IO;
using System.Linq;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests
{
    public class DirectoryServiceTests
    {
        // Path.GetDirectoryName, which Invalidate uses to find the parent, normalizes the
        // separators, so cache keys only match the parent it returns when they use the platform's.
        private static readonly string _lowerCasePath = LocalPath("/music/someartist");
        private static readonly string _upperCasePath = LocalPath("/music/SOMEARTIST");

        private static readonly FileSystemMetadata[] _lowerCaseFileSystemMetadata =
        {
            new()
            {
                FullName = Path.Combine(_lowerCasePath, "Artwork"),
                IsDirectory = true
            },
            new()
            {
                FullName = Path.Combine(_lowerCasePath, "Some Other Folder"),
                IsDirectory = true
            },
            new()
            {
                FullName = Path.Combine(_lowerCasePath, "Song 2.mp3"),
                IsDirectory = false
            },
            new()
            {
                FullName = Path.Combine(_lowerCasePath, "Song 3.mp3"),
                IsDirectory = false
            }
        };

        private static readonly FileSystemMetadata[] _upperCaseFileSystemMetadata =
        {
            new()
            {
                FullName = Path.Combine(_upperCasePath, "Lyrics"),
                IsDirectory = true
            },
            new()
            {
                FullName = Path.Combine(_upperCasePath, "Song 1.mp3"),
                IsDirectory = false
            }
        };

        [Fact]
        public void GetFileSystemEntries_GivenPathsWithDifferentCasing_CachesAll()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.Is<string>(x => x == _upperCasePath), false)).Returns(_upperCaseFileSystemMetadata);
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.Is<string>(x => x == _lowerCasePath), false)).Returns(_lowerCaseFileSystemMetadata);
            var directoryService = new DirectoryService(fileSystemMock.Object);

            var upperCaseResult = directoryService.GetFileSystemEntries(_upperCasePath);
            var lowerCaseResult = directoryService.GetFileSystemEntries(_lowerCasePath);

            Assert.Equal(_upperCaseFileSystemMetadata, upperCaseResult);
            Assert.Equal(_lowerCaseFileSystemMetadata, lowerCaseResult);
        }

        [Fact]
        public void GetFiles_GivenPathsWithDifferentCasing_ReturnsCorrectFiles()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.Is<string>(x => x == _upperCasePath), false)).Returns(_upperCaseFileSystemMetadata);
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.Is<string>(x => x == _lowerCasePath), false)).Returns(_lowerCaseFileSystemMetadata);
            var directoryService = new DirectoryService(fileSystemMock.Object);

            var upperCaseResult = directoryService.GetFiles(_upperCasePath);
            var lowerCaseResult = directoryService.GetFiles(_lowerCasePath);

            Assert.Equal(_upperCaseFileSystemMetadata.Where(f => !f.IsDirectory), upperCaseResult);
            Assert.Equal(_lowerCaseFileSystemMetadata.Where(f => !f.IsDirectory), lowerCaseResult);
        }

        [Fact]
        public void GetDirectories_GivenPathsWithDifferentCasing_ReturnsCorrectDirectories()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.Is<string>(x => x == _upperCasePath), false)).Returns(_upperCaseFileSystemMetadata);
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.Is<string>(x => x == _lowerCasePath), false)).Returns(_lowerCaseFileSystemMetadata);
            var directoryService = new DirectoryService(fileSystemMock.Object);

            var upperCaseResult = directoryService.GetDirectories(_upperCasePath);
            var lowerCaseResult = directoryService.GetDirectories(_lowerCasePath);

            Assert.Equal(_upperCaseFileSystemMetadata.Where(f => f.IsDirectory), upperCaseResult);
            Assert.Equal(_lowerCaseFileSystemMetadata.Where(f => f.IsDirectory), lowerCaseResult);
        }

        [Fact]
        public void GetFile_GivenFilePathsWithDifferentCasing_ReturnsCorrectFile()
        {
            const string lowerCasePath = "/music/someartist/song 1.mp3";
            var lowerCaseFileSystemMetadata = new FileSystemMetadata
            {
                FullName = lowerCasePath,
                Exists = true
            };
            const string upperCasePath = "/music/SOMEARTIST/SONG 1.mp3";
            var upperCaseFileSystemMetadata = new FileSystemMetadata
            {
                FullName = upperCasePath,
                Exists = false
            };
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemInfo(It.Is<string>(x => x == upperCasePath))).Returns(upperCaseFileSystemMetadata);
            fileSystemMock.Setup(f => f.GetFileSystemInfo(It.Is<string>(x => x == lowerCasePath))).Returns(lowerCaseFileSystemMetadata);
            var directoryService = new DirectoryService(fileSystemMock.Object);

            var lowerCaseDirResult = directoryService.GetDirectory(lowerCasePath);
            var lowerCaseFileResult = directoryService.GetFile(lowerCasePath);
            var upperCaseDirResult = directoryService.GetDirectory(upperCasePath);
            var upperCaseFileResult = directoryService.GetFile(upperCasePath);

            Assert.Null(lowerCaseDirResult);
            Assert.Equal(lowerCaseFileSystemMetadata, lowerCaseFileResult);
            Assert.Null(upperCaseDirResult);
            Assert.Null(upperCaseFileResult);
        }

        [Fact]
        public void GetDirectory_GivenFilePathsWithDifferentCasing_ReturnsCorrectDirectory()
        {
            const string lowerCasePath = "/music/someartist/Lyrics";
            var lowerCaseFileSystemMetadata = new FileSystemMetadata
            {
                FullName = lowerCasePath,
                IsDirectory = true,
                Exists = true
            };
            const string upperCasePath = "/music/SOMEARTIST/LYRICS";
            var upperCaseFileSystemMetadata = new FileSystemMetadata
            {
                FullName = upperCasePath,
                IsDirectory = true,
                Exists = false
            };
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemInfo(It.Is<string>(x => x == upperCasePath))).Returns(upperCaseFileSystemMetadata);
            fileSystemMock.Setup(f => f.GetFileSystemInfo(It.Is<string>(x => x == lowerCasePath))).Returns(lowerCaseFileSystemMetadata);
            var directoryService = new DirectoryService(fileSystemMock.Object);

            var lowerCaseDirResult = directoryService.GetDirectory(lowerCasePath);
            var lowerCaseFileResult = directoryService.GetFile(lowerCasePath);
            var upperCaseDirResult = directoryService.GetDirectory(upperCasePath);
            var upperCaseFileResult = directoryService.GetFile(upperCasePath);

            Assert.Equal(lowerCaseFileSystemMetadata, lowerCaseDirResult);
            Assert.Null(lowerCaseFileResult);
            Assert.Null(upperCaseDirResult);
            Assert.Null(upperCaseFileResult);
        }

        [Fact]
        public void GetFile_GivenCachedPath_ReturnsCachedFile()
        {
            const string path = "/music/someartist/song 1.mp3";
            var cachedFileSystemMetadata = new FileSystemMetadata
            {
                FullName = path,
                Exists = true
            };
            var newFileSystemMetadata = new FileSystemMetadata
            {
                FullName = "/music/SOMEARTIST/song 1.mp3",
                Exists = true
            };

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemInfo(It.Is<string>(x => x == path))).Returns(cachedFileSystemMetadata);
            var directoryService = new DirectoryService(fileSystemMock.Object);

            var result = directoryService.GetFile(path);
            fileSystemMock.Setup(f => f.GetFileSystemInfo(It.Is<string>(x => x == path))).Returns(newFileSystemMetadata);
            var secondResult = directoryService.GetFile(path);

            Assert.Equivalent(cachedFileSystemMetadata, result);
            Assert.Equivalent(cachedFileSystemMetadata, secondResult);
        }

        [Fact]
        public void GetFilePaths_GivenCachedFilePathWithoutClear_ReturnsOnlyCachedPaths()
        {
            const string path = "/music/someartist";

            var cachedPaths = new[]
            {
                "/music/someartist/song 1.mp3",
                "/music/someartist/song 2.mp3",
                "/music/someartist/song 3.mp3",
                "/music/someartist/song 4.mp3",
            };
            var newPaths = new[]
            {
                "/music/someartist/song 5.mp3",
                "/music/someartist/song 6.mp3",
                "/music/someartist/song 7.mp3",
                "/music/someartist/song 8.mp3",
            };

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFilePaths(It.Is<string>(x => x == path), false)).Returns(cachedPaths);
            var directoryService = new DirectoryService(fileSystemMock.Object);

            var result = directoryService.GetFilePaths(path);
            fileSystemMock.Setup(f => f.GetFilePaths(It.Is<string>(x => x == path), false)).Returns(newPaths);
            var secondResult = directoryService.GetFilePaths(path);

            Assert.Equal(cachedPaths, result);
            Assert.Equal(cachedPaths, secondResult);
        }

        [Fact]
        public void GetFilePaths_GivenCachedFilePathWithClear_ReturnsNewPaths()
        {
            const string path = "/music/someartist";

            var cachedPaths = new[]
            {
                "/music/someartist/song 1.mp3",
                "/music/someartist/song 2.mp3",
                "/music/someartist/song 3.mp3",
                "/music/someartist/song 4.mp3",
            };
            var newPaths = new[]
            {
                "/music/someartist/song 5.mp3",
                "/music/someartist/song 6.mp3",
                "/music/someartist/song 7.mp3",
                "/music/someartist/song 8.mp3",
            };

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFilePaths(It.Is<string>(x => x == path), false)).Returns(cachedPaths);
            var directoryService = new DirectoryService(fileSystemMock.Object);

            var result = directoryService.GetFilePaths(path);
            fileSystemMock.Setup(f => f.GetFilePaths(It.Is<string>(x => x == path), false)).Returns(newPaths);
            var secondResult = directoryService.GetFilePaths(path, true);

            Assert.Equal(cachedPaths, result);
            Assert.Equal(newPaths, secondResult);
        }

        [Fact]
        public void GetFileSystemEntries_RepeatedPath_ReadsTheFileSystemOnce()
        {
            var fileSystemMock = new Mock<IFileSystem>(MockBehavior.Strict);
            fileSystemMock.Setup(f => f.GetFileSystemEntries(_lowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata);

            var directoryService = new DirectoryService(fileSystemMock.Object);

            directoryService.GetFileSystemEntries(_lowerCasePath);
            directoryService.GetFileSystemEntries(_lowerCasePath);

            fileSystemMock.Verify(f => f.GetFileSystemEntries(_lowerCasePath), Times.Once);
        }

        [Fact]
        public void Invalidate_GivenADirectory_DropsBothTheListingAndTheFilePaths()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.SetupSequence(f => f.GetFileSystemEntries(_lowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata)
                .Returns(_upperCaseFileSystemMetadata);
            fileSystemMock.SetupSequence(f => f.GetFilePaths(_lowerCasePath, false))
                .Returns(new[] { Path.Combine(_lowerCasePath, "Song 2.mp3") })
                .Returns(new[] { Path.Combine(_lowerCasePath, "Song 2.mp3"), Path.Combine(_lowerCasePath, "Song 2.srt") });

            var directoryService = new DirectoryService(fileSystemMock.Object);
            directoryService.GetFileSystemEntries(_lowerCasePath);
            directoryService.GetFilePaths(_lowerCasePath);

            directoryService.Invalidate(_lowerCasePath);

            Assert.Equal(_upperCaseFileSystemMetadata, directoryService.GetFileSystemEntries(_lowerCasePath));
            Assert.Equal(2, directoryService.GetFilePaths(_lowerCasePath).Count);
        }

        [Fact]
        public void Invalidate_GivenAFile_DropsTheListingOfTheDirectoryHoldingIt()
        {
            var newFile = Path.Combine(_lowerCasePath, "Song 2.srt");

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.SetupSequence(f => f.GetFileSystemEntries(_lowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata)
                .Returns(_upperCaseFileSystemMetadata);

            var directoryService = new DirectoryService(fileSystemMock.Object);
            directoryService.GetFileSystemEntries(_lowerCasePath);

            directoryService.Invalidate(newFile);

            Assert.Equal(_upperCaseFileSystemMetadata, directoryService.GetFileSystemEntries(_lowerCasePath));
        }

        [Fact]
        public void GetFilePaths_ClearingTheCache_KeepsTheParentDirectory()
        {
            var parentPath = LocalPath("/music");

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFilePaths(_lowerCasePath))
                .Returns(new[] { Path.Combine(_lowerCasePath, "Song 2.mp3") });
            fileSystemMock.Setup(f => f.GetFileSystemEntries(parentPath))
                .Returns(_lowerCaseFileSystemMetadata);

            var directoryService = new DirectoryService(fileSystemMock.Object);
            directoryService.GetFileSystemEntries(parentPath);

            directoryService.GetFilePaths(_lowerCasePath, true);

            directoryService.GetFileSystemEntries(parentPath);
            fileSystemMock.Verify(f => f.GetFileSystemEntries(parentPath), Times.Once);
        }

        [Fact]
        public void GetFileSystemEntry_MissingPath_IsNotRemembered()
        {
            const string MissingPath = "/music/not-here";

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.SetupSequence(f => f.GetFileSystemInfo(MissingPath))
                .Returns(new FileSystemMetadata { FullName = MissingPath, Exists = false })
                .Returns(new FileSystemMetadata { FullName = MissingPath, Exists = true });

            var directoryService = new DirectoryService(fileSystemMock.Object);

            Assert.Null(directoryService.GetFileSystemEntry(MissingPath));

            Assert.NotNull(directoryService.GetFileSystemEntry(MissingPath));
        }

        private static string LocalPath(string path)
            => path.Replace('/', Path.DirectorySeparatorChar);
    }
}
