using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public void GetFileSystemEntries_MoreFoldersThanTheCapacity_EvictsTheColdest()
        {
            // Charged by the number of listings now, not the files in them, and only the coldest are
            // given up rather than the whole cache.
            const int FolderCount = 4096;
            const string FirstPath = "/music/artist0";

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.IsAny<string>()))
                .Returns(_lowerCaseFileSystemMetadata);

            var directoryService = new DirectoryService(fileSystemMock.Object);
            directoryService.GetFileSystemEntries(FirstPath);

            for (var i = 1; i < FolderCount; i++)
            {
                directoryService.GetFileSystemEntries("/music/artist" + i.ToString(CultureInfo.InvariantCulture));
            }

            var lastPath = "/music/artist" + (FolderCount - 1).ToString(CultureInfo.InvariantCulture);
            directoryService.GetFileSystemEntries(lastPath);
            directoryService.GetFileSystemEntries(FirstPath);

            fileSystemMock.Verify(f => f.GetFileSystemEntries(FirstPath), Times.Exactly(2));
            fileSystemMock.Verify(f => f.GetFileSystemEntries(lastPath), Times.Once);
        }

        [Fact]
        public void GetFileSystemEntries_SecondServiceOverTheSameFileSystem_ReusesTheListing()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(_lowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata);

            new DirectoryService(fileSystemMock.Object).GetFileSystemEntries(_lowerCasePath);
            new DirectoryService(fileSystemMock.Object).GetFileSystemEntries(_lowerCasePath);

            fileSystemMock.Verify(f => f.GetFileSystemEntries(_lowerCasePath), Times.Once);
        }

        [Fact]
        public void GetFileSystemEntries_SeparateFileSystems_DoNotShareAListing()
        {
            var firstMock = new Mock<IFileSystem>();
            firstMock.Setup(f => f.GetFileSystemEntries(_lowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata);
            var secondMock = new Mock<IFileSystem>();
            secondMock.Setup(f => f.GetFileSystemEntries(_lowerCasePath))
                .Returns(_upperCaseFileSystemMetadata);

            var first = new DirectoryService(firstMock.Object).GetFileSystemEntries(_lowerCasePath);
            var second = new DirectoryService(secondMock.Object).GetFileSystemEntries(_lowerCasePath);

            Assert.Equal(_lowerCaseFileSystemMetadata, first);
            Assert.Equal(_upperCaseFileSystemMetadata, second);
        }

        [Fact]
        public async Task GetFileSystemEntries_ConcurrentCallsForOnePath_ReadsTheFileSystemOnce()
        {
            const int Callers = 8;
            using var allCallersStarted = new CountdownEvent(Callers);

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(_lowerCasePath))
                .Returns(() =>
                {
                    // Hold the listing open until everyone has asked for it, so a cache that lets the
                    // factory run more than once has every opportunity to do so.
                    allCallersStarted.Wait(TimeSpan.FromSeconds(10));
                    return _lowerCaseFileSystemMetadata;
                });

            var directoryService = new DirectoryService(fileSystemMock.Object);

            var callers = new Task[Callers];
            for (var i = 0; i < Callers; i++)
            {
                callers[i] = Task.Run(() =>
                {
                    allCallersStarted.Signal();
                    return directoryService.GetFileSystemEntries(_lowerCasePath);
                });
            }

            await Task.WhenAll(callers);

            fileSystemMock.Verify(f => f.GetFileSystemEntries(_lowerCasePath), Times.Once);
        }

        [Fact]
        public async Task GetFileSystemEntries_AfterTheEntryLifetime_RereadsTheListing()
        {
            var lifetime = TimeSpan.FromMilliseconds(100);

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(_lowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata);

            var directoryService = new DirectoryService(fileSystemMock.Object, lifetime);
            directoryService.GetFileSystemEntries(_lowerCasePath);

            // A read discards the key it touches if it has expired, so this holds with or without a
            // trim. What the trim adds is giving the memory back when nothing reads at all, and that
            // is not observable from out here.
            await Task.Delay(lifetime * 5, TestContext.Current.CancellationToken);

            directoryService.GetFileSystemEntries(_lowerCasePath);

            fileSystemMock.Verify(f => f.GetFileSystemEntries(_lowerCasePath), Times.Exactly(2));
        }

        [Fact]
        public void TrimExpired_WithinTheEntryLifetime_KeepsTheListing()
        {
            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(_lowerCasePath))
                .Returns(_lowerCaseFileSystemMetadata);

            var directoryService = new DirectoryService(fileSystemMock.Object, TimeSpan.FromMinutes(10));
            directoryService.GetFileSystemEntries(_lowerCasePath);

            directoryService.TrimExpired();

            directoryService.GetFileSystemEntries(_lowerCasePath);

            fileSystemMock.Verify(f => f.GetFileSystemEntries(_lowerCasePath), Times.Once);
        }

        [Fact]
        public void Move_GivenACachedDirectory_ForgetsBothPaths()
        {
            var root = Directory.CreateTempSubdirectory("jellyfin-directoryservice-tests");
            try
            {
                var source = Path.Combine(root.FullName, "before");
                var destination = Path.Combine(root.FullName, "after");
                Directory.CreateDirectory(source);

                var fileSystemMock = new Mock<IFileSystem>();
                fileSystemMock.Setup(f => f.GetFileSystemEntries(It.IsAny<string>()))
                    .Returns(_lowerCaseFileSystemMetadata);

                var directoryService = new DirectoryService(fileSystemMock.Object);
                directoryService.GetFileSystemEntries(source);
                directoryService.GetFileSystemEntries(destination);
                directoryService.GetFileSystemEntries(root.FullName);

                directoryService.Move(source, destination);

                Assert.True(Directory.Exists(destination));
                Assert.False(Directory.Exists(source));

                directoryService.GetFileSystemEntries(source);
                directoryService.GetFileSystemEntries(destination);
                directoryService.GetFileSystemEntries(root.FullName);

                fileSystemMock.Verify(f => f.GetFileSystemEntries(source), Times.Exactly(2));
                fileSystemMock.Verify(f => f.GetFileSystemEntries(destination), Times.Exactly(2));
                fileSystemMock.Verify(f => f.GetFileSystemEntries(root.FullName), Times.Exactly(2));
            }
            finally
            {
                root.Delete(true);
            }
        }

        [Fact]
        public void GetFileSystemEntries_RepeatedlyInvalidatedFolder_KeepsUnrelatedEntriesCached()
        {
            // Invalidating gives the records back, so churning one folder must not add up to the
            // ceiling and drop everything else with it.
            const int ChurnCount = 50;
            var bigListing = new FileSystemMetadata[5000];
            for (var i = 0; i < bigListing.Length; i++)
            {
                bigListing[i] = new FileSystemMetadata
                {
                    FullName = "/music/track" + i.ToString(CultureInfo.InvariantCulture),
                    IsDirectory = false
                };
            }

            var fileSystemMock = new Mock<IFileSystem>();
            fileSystemMock.Setup(f => f.GetFileSystemEntries(It.IsAny<string>()))
                .Returns(bigListing);

            var directoryService = new DirectoryService(fileSystemMock.Object);

            const string ChurnedPath = "/music/watched";
            const string StablePath = "/music/untouched";
            directoryService.GetFileSystemEntries(StablePath);

            for (var i = 0; i < ChurnCount; i++)
            {
                directoryService.GetFileSystemEntries(ChurnedPath);
                directoryService.Invalidate(ChurnedPath);
            }

            directoryService.GetFileSystemEntries(StablePath);

            fileSystemMock.Verify(f => f.GetFileSystemEntries(StablePath), Times.Once);
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
