using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Moq;
using Xunit;

namespace Jellyfin.Controller.Tests
{
    public class DirectoryServiceTests
    {
        private const string LowerCasePath = "/music/someartist";
        private const string UpperCasePath = "/music/SOMEARTIST";

        private static readonly FileSystemMetadata[] _lowerCaseFileSystemMetadata =
        {
            new()
            {
                FullName = LowerCasePath + "/Artwork",
                IsDirectory = true
            },
            new()
            {
                FullName = LowerCasePath + "/Some Other Folder",
                IsDirectory = true
            },
            new()
            {
                FullName = LowerCasePath + "/Song 2.mp3",
                IsDirectory = false
            },
            new()
            {
                FullName = LowerCasePath + "/Song 3.mp3",
                IsDirectory = false
            }
        };

        private static readonly FileSystemMetadata[] _upperCaseFileSystemMetadata =
        {
            new()
            {
                FullName = UpperCasePath + "/Lyrics",
                IsDirectory = true
            },
            new()
            {
                FullName = UpperCasePath + "/Song 1.mp3",
                IsDirectory = false
            }
        };

        [Fact]
        public void GetFileSystemEntries_GivenPathsWithDifferentCasing_CachesAll()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.Is<string>(x => x == UpperCasePath), false)).Returns(_upperCaseFileSystemMetadata);
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.Is<string>(x => x == LowerCasePath), false)).Returns(_lowerCaseFileSystemMetadata);
            var directoryService = new DirectoryService(fileSystemMock.Object);

            var upperCaseResult = directoryService.GetFileSystemEntries(UpperCasePath);
            var lowerCaseResult = directoryService.GetFileSystemEntries(LowerCasePath);

            Assert.Equal(_upperCaseFileSystemMetadata, upperCaseResult);
            Assert.Equal(_lowerCaseFileSystemMetadata, lowerCaseResult);
        }

        [Fact]
        public void GetFiles_GivenPathsWithDifferentCasing_ReturnsCorrectFiles()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.Is<string>(x => x == UpperCasePath), false)).Returns(_upperCaseFileSystemMetadata);
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.Is<string>(x => x == LowerCasePath), false)).Returns(_lowerCaseFileSystemMetadata);
            var directoryService = new DirectoryService(fileSystemMock.Object);

            var upperCaseResult = directoryService.GetFiles(UpperCasePath);
            var lowerCaseResult = directoryService.GetFiles(LowerCasePath);

            Assert.Equal(_upperCaseFileSystemMetadata.Where(f => !f.IsDirectory), upperCaseResult);
            Assert.Equal(_lowerCaseFileSystemMetadata.Where(f => !f.IsDirectory), lowerCaseResult);
        }

        [Fact]
        public void GetDirectories_GivenPathsWithDifferentCasing_ReturnsCorrectDirectories()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.Is<string>(x => x == UpperCasePath), false)).Returns(_upperCaseFileSystemMetadata);
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.Is<string>(x => x == LowerCasePath), false)).Returns(_lowerCaseFileSystemMetadata);
            var directoryService = new DirectoryService(fileSystemMock.Object);

            var upperCaseResult = directoryService.GetDirectories(UpperCasePath);
            var lowerCaseResult = directoryService.GetDirectories(LowerCasePath);

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
            fileSystemMock.Setup(f => f.GetFileSystemEntries(LowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata);

            var directoryService = new DirectoryService(fileSystemMock.Object);

            directoryService.GetFileSystemEntries(LowerCasePath);
            directoryService.GetFileSystemEntries(LowerCasePath);

            fileSystemMock.Verify(f => f.GetFileSystemEntries(LowerCasePath), Times.Once);
        }

        [Fact]
        public void GetFileSystemEntries_FarMorePathsThanTheCacheHolds_EvictsInsteadOfGrowing()
        {
            // Eviction of the first path shows up as the file system being read for it twice.
            const int PathCount = 8192;

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.IsAny<string>()))
                .Returns(_lowerCaseFileSystemMetadata);

            var directoryService = new DirectoryService(fileSystemMock.Object);

            const string FirstPath = "/music/artist0";
            directoryService.GetFileSystemEntries(FirstPath);

            for (var i = 1; i < PathCount; i++)
            {
                directoryService.GetFileSystemEntries("/music/artist" + i.ToString(CultureInfo.InvariantCulture));
            }

            directoryService.GetFileSystemEntries(FirstPath);

            fileSystemMock.Verify(f => f.GetFileSystemEntries(FirstPath), Times.Exactly(2));
        }

        [Fact]
        public void GetFileSystemEntries_SecondServiceOverSameFileSystem_ReusesTheFirstAnswer()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(LowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata);

            new DirectoryService(fileSystemMock.Object).GetFileSystemEntries(LowerCasePath);
            var result = new DirectoryService(fileSystemMock.Object).GetFileSystemEntries(LowerCasePath);

            Assert.Equal(_lowerCaseFileSystemMetadata, result);
            fileSystemMock.Verify(f => f.GetFileSystemEntries(LowerCasePath), Times.Once);
        }

        [Fact]
        public void GetFileSystemEntries_SeparateFileSystems_DoNotShareAnswers()
        {
            var firstFileSystem = new Mock<IFileSystem>();
            firstFileSystem.Setup(f => f.GetFileSystemEntries(LowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata);
            var secondFileSystem = new Mock<IFileSystem>();
            secondFileSystem.Setup(f => f.GetFileSystemEntries(LowerCasePath))
                .Returns(_upperCaseFileSystemMetadata);

            var firstResult = new DirectoryService(firstFileSystem.Object).GetFileSystemEntries(LowerCasePath);
            var secondResult = new DirectoryService(secondFileSystem.Object).GetFileSystemEntries(LowerCasePath);

            Assert.Equal(_lowerCaseFileSystemMetadata, firstResult);
            Assert.Equal(_upperCaseFileSystemMetadata, secondResult);
        }

        [Fact]
        public void Invalidate_GivenADirectory_DropsBothTheListingAndTheFilePaths()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.SetupSequence(f => f.GetFileSystemEntries(LowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata)
                .Returns(_upperCaseFileSystemMetadata);
            fileSystemMock.SetupSequence(f => f.GetFilePaths(LowerCasePath, false))
                .Returns(new[] { LowerCasePath + "/Song 2.mp3" })
                .Returns(new[] { LowerCasePath + "/Song 2.mp3", LowerCasePath + "/Song 2.srt" });

            var directoryService = new DirectoryService(fileSystemMock.Object);
            directoryService.GetFileSystemEntries(LowerCasePath);
            directoryService.GetFilePaths(LowerCasePath);

            directoryService.Invalidate(LowerCasePath);

            Assert.Equal(_upperCaseFileSystemMetadata, directoryService.GetFileSystemEntries(LowerCasePath));
            Assert.Equal(2, directoryService.GetFilePaths(LowerCasePath).Count);
        }

        [Fact]
        public void Invalidate_GivenAFile_DropsTheListingOfTheDirectoryHoldingIt()
        {
            const string NewFile = LowerCasePath + "/Song 2.srt";

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.SetupSequence(f => f.GetFileSystemEntries(LowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata)
                .Returns(_upperCaseFileSystemMetadata);

            var directoryService = new DirectoryService(fileSystemMock.Object);
            directoryService.GetFileSystemEntries(LowerCasePath);

            directoryService.Invalidate(NewFile);

            Assert.Equal(_upperCaseFileSystemMetadata, directoryService.GetFileSystemEntries(LowerCasePath));
        }

        [Fact]
        public void Invalidate_OnOneService_IsSeenByAnotherOverTheSameFileSystem()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.SetupSequence(f => f.GetFileSystemEntries(LowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata)
                .Returns(_upperCaseFileSystemMetadata);

            new DirectoryService(fileSystemMock.Object).GetFileSystemEntries(LowerCasePath);
            new DirectoryService(fileSystemMock.Object).Invalidate(LowerCasePath + "/Song 2.srt");

            var result = new DirectoryService(fileSystemMock.Object).GetFileSystemEntries(LowerCasePath);

            Assert.Equal(_upperCaseFileSystemMetadata, result);
        }

        [Fact]
        public void GetFilePaths_ClearingTheCache_KeepsTheParentDirectory()
        {
            const string ParentPath = "/music";

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFilePaths(LowerCasePath))
                .Returns(new[] { LowerCasePath + "/Song 2.mp3" });
            fileSystemMock.Setup(f => f.GetFileSystemEntries(ParentPath))
                .Returns(_lowerCaseFileSystemMetadata);

            var directoryService = new DirectoryService(fileSystemMock.Object);
            directoryService.GetFileSystemEntries(ParentPath);

            directoryService.GetFilePaths(LowerCasePath, true);

            directoryService.GetFileSystemEntries(ParentPath);
            fileSystemMock.Verify(f => f.GetFileSystemEntries(ParentPath), Times.Once);
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
    }
}
