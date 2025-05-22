using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using MediaBrowser.Controller.Resolvers; // For IItemResolver and related types
using MediaBrowser.Controller.Dto;        // For DtoOptions
using Jellyfin.Data.Interfaces; // For IItemRepository (if that's where it is)
using MediaBrowser.Controller.Persistence; // For IItemRepository (if that's where it is)


namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class LibraryManagerUploadTests
    {
        private readonly Mock<IServerConfigurationManager> _mockConfigManager;
        private readonly Mock<ILogger<LibraryManager>> _mockLogger;
        private readonly Mock<IFileSystem> _mockFileSystem;
        private readonly Mock<ILibraryMonitor> _mockLibraryMonitor;
        private readonly Mock<IProviderManager> _mockProviderManager;
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<IDirectoryService> _mockDirectoryService;
        private readonly LibraryManager _libraryManager;
        private readonly ServerApplicationPaths _serverApplicationPaths;
        private readonly NamingOptions _namingOptions;

        // Helper to create IFormFile
        private IFormFile CreateTestFormFile(string fileName, string content)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            return new FormFile(stream, 0, stream.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/octet-stream"
            };
        }

        public LibraryManagerUploadTests()
        {
            _mockConfigManager = new Mock<IServerConfigurationManager>();
            _mockLogger = new Mock<ILogger<LibraryManager>>();
            _mockFileSystem = new Mock<IFileSystem>();
            _mockLibraryMonitor = new Mock<ILibraryMonitor>();
            _mockProviderManager = new Mock<IProviderManager>();
            _mockItemRepository = new Mock<IItemRepository>();
            _mockDirectoryService = new Mock<IDirectoryService>();
            _namingOptions = new NamingOptions();

            _serverApplicationPaths = new ServerApplicationPaths
            {
                RootFolderPath = "/app/root",
                DefaultUserViewsPath = "/app/user_views",
                ProgramDataPath = "/app/programdata"
            };

            _mockConfigManager.Setup(c => c.ApplicationPaths).Returns(_serverApplicationPaths);
            _mockConfigManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());


            // Mock IServerApplicationHost for LibraryManager constructor
            var mockAppHost = new Mock<IServerApplicationHost>();
            mockAppHost.Setup(ah => ah.ExpandVirtualPath(It.IsAny<string>())).Returns<string>(p => p);
            mockAppHost.Setup(ah => ah.ReverseVirtualPath(It.IsAny<string>())).Returns<string>(p => p);


            // Mock ILibraryPostScanTask, IIntroProvider, IResolverIgnoreRule, IItemResolver, IBaseItemComparer for AddParts
            var mockLoggerFactory = new NullLoggerFactory();


            _libraryManager = new LibraryManager(
                mockAppHost.Object,
                mockLoggerFactory,
                new Mock<MediaBrowser.Model.Tasks.ITaskManager>().Object,
                new Mock<IUserManager>().Object,
                _mockConfigManager.Object,
                new Mock<IUserDataManager>().Object,
                new Lazy<ILibraryMonitor>(() => _mockLibraryMonitor.Object),
                _mockFileSystem.Object,
                new Lazy<IProviderManager>(() => _mockProviderManager.Object),
                new Lazy<IUserViewManager>(() => new Mock<IUserViewManager>().Object),
                new Mock<MediaBrowser.Controller.MediaEncoding.IMediaEncoder>().Object,
                _mockItemRepository.Object,
                new Mock<MediaBrowser.Controller.Drawing.IImageProcessor>().Object,
                _namingOptions,
                _mockDirectoryService.Object, // Corrected: Use the mocked IDirectoryService
                new Mock<IPeopleRepository>().Object,
                new Mock<MediaBrowser.Controller.Providers.IPathManager>().Object
            );

            // Call AddParts with empty arrays for simplicity, or mock specific parts if needed by tests
            _libraryManager.AddParts(
                Array.Empty<IResolverIgnoreRule>(),
                Array.Empty<IItemResolver>(),
                Array.Empty<IIntroProvider>(),
                Array.Empty<IBaseItemComparer>(),
                Array.Empty<ILibraryPostScanTask>()
            );
        }

        private CollectionFolder SetupMockLibrary(string libraryName, Guid libraryId, string physicalPath, CollectionType? collectionType = null)
        {
            var library = new Mock<CollectionFolder>();
            library.Setup(l => l.Id).Returns(libraryId);
            library.Setup(l => l.Name).Returns(libraryName);
            library.Setup(l => l.Path).Returns(physicalPath); // The "view" path
            library.Setup(l => l.PhysicalLocations).Returns(new[] { physicalPath }); // Actual disk path
            library.Setup(l => l.CollectionType).Returns(collectionType);
            library.Setup(l => l.GetPhysicalFolderPath()).Returns(physicalPath);

            _mockFileSystem.Setup(fs => fs.DirectoryExists(physicalPath)).Returns(true);
             _mockFileSystem.Setup(fs => fs.GetDirectoryInfo(physicalPath)).Returns(new FileSystemMetadata { FullName = physicalPath, IsDirectory = true });


            return library.Object;
        }


        [Fact]
        public async Task AddUploadedMediaFile_WithLibraryId_SavesFileAndRefreshes()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var libraryPath = "/app/user_views/Movies";
            var fileName = "movie.mp4";
            var fullExpectedPath = Path.Combine(libraryPath, fileName);

            var mockLibrary = SetupMockLibrary("Movies", libraryId, libraryPath, CollectionType.Movies);

            var userRootFolderMock = new Mock<UserRootFolder>();
            userRootFolderMock.Setup(urf => urf.Children).Returns(new List<BaseItem> { mockLibrary });
            userRootFolderMock.Setup(x => x.Path).Returns(_serverApplicationPaths.DefaultUserViewsPath);
            userRootFolderMock.Setup(x => x.Id).Returns(Guid.NewGuid());


            // Mock GetUserRootFolder to return our mocked UserRootFolder
            // This requires making GetUserRootFolder public or refactoring how it's accessed for testing.
            // For now, assume it's mockable or refactored. Let's simulate its behavior by setting up item repo.
             _mockItemRepository.Setup(repo => repo.RetrieveItem(It.Is<Guid>(g => g != Guid.Empty && g != libraryId))) // any Guid that is not libraryId
                .Returns(userRootFolderMock.Object);
            _mockItemRepository.Setup(repo => repo.RetrieveItem(libraryId)).Returns(mockLibrary);


            _mockFileSystem.Setup(fs => fs.FileExists(fullExpectedPath)).Returns(false);
            _mockFileSystem.Setup(fs => fs.GetFileAttributes(fullExpectedPath)).Throws<FileNotFoundException>();
            _mockFileSystem.Setup(fs => fs.CreateDirectory(libraryPath)); // Ensure CreateDirectory is set up

            var formFile = CreateTestFormFile(fileName, "movie content");

            // Act
            var result = await _libraryManager.AddUploadedMediaFile(formFile, libraryId, null);

            // Assert
            Assert.True(result);
            _mockFileSystem.Verify(fs => fs.Create(fullExpectedPath, It.IsAny<FileMode>(), It.IsAny<FileShare>(), It.IsAny<FileOptions>()), Times.Never); // Should be using FileStream
            _mockLibraryMonitor.Verify(lm => lm.ReportFileSystemChange(fullExpectedPath, false), Times.Once);
            _mockProviderManager.Verify(pm => pm.QueueRefresh(libraryId, It.IsAny<MetadataRefreshOptions>(), RefreshPriority.Normal), Times.Once);
        }


        [Fact]
        public async Task AddUploadedMediaFile_WithCollectionType_SavesFileAndRefreshes()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var libraryPath = "/app/user_views/TVShows";
            var collectionType = "tvshows"; // Case-insensitive match is important
            var fileName = "episode.mkv";
            var fullExpectedPath = Path.Combine(libraryPath, fileName);

            var mockLibrary = SetupMockLibrary("TV Shows", libraryId, libraryPath, CollectionType.TvShows);

            var userRootFolderMock = new Mock<UserRootFolder>();
            userRootFolderMock.Setup(urf => urf.Children).Returns(new List<BaseItem> { mockLibrary });
            userRootFolderMock.Setup(x => x.Path).Returns(_serverApplicationPaths.DefaultUserViewsPath);
            userRootFolderMock.Setup(x => x.Id).Returns(Guid.NewGuid());

             _mockItemRepository.Setup(repo => repo.RetrieveItem(It.Is<Guid>(g => g != Guid.Empty && g != libraryId)))
                .Returns(userRootFolderMock.Object);
             _mockItemRepository.Setup(repo => repo.RetrieveItem(libraryId)).Returns(mockLibrary);


            _mockFileSystem.Setup(fs => fs.FileExists(fullExpectedPath)).Returns(false);
            _mockFileSystem.Setup(fs => fs.GetFileAttributes(fullExpectedPath)).Throws<FileNotFoundException>();
             _mockFileSystem.Setup(fs => fs.CreateDirectory(libraryPath));


            var formFile = CreateTestFormFile(fileName, "tv content");

            // Act
            var result = await _libraryManager.AddUploadedMediaFile(formFile, null, collectionType);

            // Assert
            Assert.True(result);
            _mockLibraryMonitor.Verify(lm => lm.ReportFileSystemChange(fullExpectedPath, false), Times.Once);
            _mockProviderManager.Verify(pm => pm.QueueRefresh(libraryId, It.IsAny<MetadataRefreshOptions>(), RefreshPriority.Normal), Times.Once);
        }

        [Fact]
        public async Task AddUploadedMediaFile_NoLibraryIdOrCollectionType_UsesDefaultAndSaves()
        {
            // Arrange
            var defaultLibraryId = Guid.NewGuid();
            var defaultLibraryPath = "/app/user_views/DefaultUploads";
            var fileName = "default.dat";
            var fullExpectedPath = Path.Combine(defaultLibraryPath, fileName);

            var mockDefaultLibrary = SetupMockLibrary("Default Uploads", defaultLibraryId, defaultLibraryPath, null); // No specific collection type

            var userRootFolderMock = new Mock<UserRootFolder>();
            userRootFolderMock.Setup(urf => urf.Children).Returns(new List<BaseItem> { mockDefaultLibrary });
             userRootFolderMock.Setup(x => x.Path).Returns(_serverApplicationPaths.DefaultUserViewsPath);
            userRootFolderMock.Setup(x => x.Id).Returns(Guid.NewGuid());

            _mockItemRepository.Setup(repo => repo.RetrieveItem(It.Is<Guid>(g => g != Guid.Empty && g != defaultLibraryId)))
                .Returns(userRootFolderMock.Object);
            _mockItemRepository.Setup(repo => repo.RetrieveItem(defaultLibraryId)).Returns(mockDefaultLibrary);


            _mockFileSystem.Setup(fs => fs.FileExists(fullExpectedPath)).Returns(false);
            _mockFileSystem.Setup(fs => fs.GetFileAttributes(fullExpectedPath)).Throws<FileNotFoundException>();
             _mockFileSystem.Setup(fs => fs.CreateDirectory(defaultLibraryPath));

            var formFile = CreateTestFormFile(fileName, "default content");

            // Act
            var result = await _libraryManager.AddUploadedMediaFile(formFile, null, null);

            // Assert
            Assert.True(result);
            _mockLibraryMonitor.Verify(lm => lm.ReportFileSystemChange(fullExpectedPath, false), Times.Once);
            _mockProviderManager.Verify(pm => pm.QueueRefresh(defaultLibraryId, It.IsAny<MetadataRefreshOptions>(), RefreshPriority.Normal), Times.Once);
        }

        [Fact]
        public async Task AddUploadedMediaFile_NullFile_ReturnsFalse()
        {
            // Arrange
            // Act
            var result = await _libraryManager.AddUploadedMediaFile(null!, null, null);

            // Assert
            Assert.False(result);
            // Verify logger was called with an error
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Uploaded file is null or empty.")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task AddUploadedMediaFile_EmptyFile_ReturnsFalse()
        {
            // Arrange
            var emptyFile = CreateTestFormFile("empty.txt", "");
            // Ensure the length is 0
            var formFile = new FormFile(new MemoryStream(), 0, 0, "file", "empty.txt");


            // Act
            var result = await _libraryManager.AddUploadedMediaFile(formFile, Guid.NewGuid(), "movies");

            // Assert
            Assert.False(result);
             _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Uploaded file is null or empty.")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }


        [Fact]
        public async Task AddUploadedMediaFile_NoSuitableLibraryFound_ReturnsFalse()
        {
            // Arrange
            var userRootFolderMock = new Mock<UserRootFolder>();
            userRootFolderMock.Setup(urf => urf.Children).Returns(new List<BaseItem>()); // No libraries
            userRootFolderMock.Setup(x => x.Path).Returns(_serverApplicationPaths.DefaultUserViewsPath);
             userRootFolderMock.Setup(x => x.Id).Returns(Guid.NewGuid());


            _mockItemRepository.Setup(repo => repo.RetrieveItem(It.IsAny<Guid>())).Returns(userRootFolderMock.Object);


            var formFile = CreateTestFormFile("somefile.zip", "archive data");

            // Act
            var result = await _libraryManager.AddUploadedMediaFile(formFile, null, "nonexistenttype");

            // Assert
            Assert.False(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("No libraries available to upload the file.") || v.ToString().Contains("Could not determine a valid target library path")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task AddUploadedMediaFile_IOExceptionDuringSave_ReturnsFalseAndLogsError()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var libraryPath = "/app/user_views/Photos";
            var fileName = "photo.jpg";
            var fullExpectedPath = Path.Combine(libraryPath, fileName);

            var mockLibrary = SetupMockLibrary("Photos", libraryId, libraryPath, CollectionType.Photos);

            var userRootFolderMock = new Mock<UserRootFolder>();
            userRootFolderMock.Setup(urf => urf.Children).Returns(new List<BaseItem> { mockLibrary });
            userRootFolderMock.Setup(x => x.Path).Returns(_serverApplicationPaths.DefaultUserViewsPath);
            userRootFolderMock.Setup(x => x.Id).Returns(Guid.NewGuid());

            _mockItemRepository.Setup(repo => repo.RetrieveItem(It.Is<Guid>(g => g != Guid.Empty && g != libraryId)))
                .Returns(userRootFolderMock.Object);
            _mockItemRepository.Setup(repo => repo.RetrieveItem(libraryId)).Returns(mockLibrary);

            // Simulate IOException when trying to create the FileStream
            // This is tricky because the FileStream is created inside the method.
            // One way is to make IFileSystem.OpenWrite mockable and throw from there.
            // For now, let's assume the path itself causes an issue that File.Create would throw for.
            // A more direct mock would involve refactoring LibraryManager to use an injectable factory for FileStream, or IFileSystem.WriteAllBytesAsync etc.
            _mockFileSystem.Setup(fs => fs.CreateDirectory(libraryPath)); // This should succeed

            // To simulate FileStream creation failure, we can make the path invalid *after* directory checks
            // This is still indirect. The ideal way is to have an abstraction for file system write operations.
            // For this example, we'll assume the CopyToAsync throws an IOException.
            var formFileMock = new Mock<IFormFile>();
            formFileMock.Setup(f => f.FileName).Returns(fileName);
            formFileMock.Setup(f => f.Length).Returns(100); // Non-empty
            formFileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new IOException("Disk full"));


            // Act
            var result = await _libraryManager.AddUploadedMediaFile(formFileMock.Object, libraryId, null);

            // Assert
            Assert.False(result);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("IO Error saving uploaded file")),
                    It.IsAny<IOException>(), // Check that an IOException is logged
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task AddUploadedMediaFile_SanitizesFileName()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var libraryPath = "/app/user_views/Music";
            var originalFileName = "bad<name>:.mp3";
            var sanitizedFileName = "bad_name_.mp3"; // Based on current sanitization logic
            var fullExpectedPath = Path.Combine(libraryPath, sanitizedFileName);

            var mockLibrary = SetupMockLibrary("Music", libraryId, libraryPath, CollectionType.Music);

            var userRootFolderMock = new Mock<UserRootFolder>();
            userRootFolderMock.Setup(urf => urf.Children).Returns(new List<BaseItem> { mockLibrary });
            userRootFolderMock.Setup(x => x.Path).Returns(_serverApplicationPaths.DefaultUserViewsPath);
            userRootFolderMock.Setup(x => x.Id).Returns(Guid.NewGuid());

            _mockItemRepository.Setup(repo => repo.RetrieveItem(It.Is<Guid>(g => g != Guid.Empty && g != libraryId)))
                .Returns(userRootFolderMock.Object);
            _mockItemRepository.Setup(repo => repo.RetrieveItem(libraryId)).Returns(mockLibrary);

            _mockFileSystem.Setup(fs => fs.FileExists(fullExpectedPath)).Returns(false);
             _mockFileSystem.Setup(fs => fs.CreateDirectory(libraryPath));

            var formFile = CreateTestFormFile(originalFileName, "music content");

            // Act
            var result = await _libraryManager.AddUploadedMediaFile(formFile, libraryId, null);

            // Assert
            Assert.True(result);
            // Verify that the file was attempted to be saved with the sanitized name
            // This relies on the FileStream being created with `Path.Combine(targetLibraryPath, sanitizedFileName)`
            // As FileStream creation is not directly mocked, this is an indirect verification via logger or other side effects if available.
            // For now, we assume success implies correct path combination.
            _mockLibraryMonitor.Verify(lm => lm.ReportFileSystemChange(fullExpectedPath, false), Times.Once);
        }

        [Fact]
        public async Task AddUploadedMediaFile_DirectoryDoesNotExist_CreatesDirectory()
        {
            // Arrange
            var libraryId = Guid.NewGuid();
            var libraryPath = "/app/user_views/NewFolder";
            var fileName = "file.txt";
            var fullExpectedPath = Path.Combine(libraryPath, fileName);

            var mockLibrary = SetupMockLibrary("New Lib", libraryId, libraryPath, null);
            mockLibrary.Setup(l => l.PhysicalLocations).Returns(new[] { libraryPath }); // Override

            var userRootFolderMock = new Mock<UserRootFolder>();
            userRootFolderMock.Setup(urf => urf.Children).Returns(new List<BaseItem> { mockLibrary });
            userRootFolderMock.Setup(x => x.Path).Returns(_serverApplicationPaths.DefaultUserViewsPath);
            userRootFolderMock.Setup(x => x.Id).Returns(Guid.NewGuid());

            _mockItemRepository.Setup(repo => repo.RetrieveItem(It.Is<Guid>(g => g != Guid.Empty && g != libraryId)))
                .Returns(userRootFolderMock.Object);
             _mockItemRepository.Setup(repo => repo.RetrieveItem(libraryId)).Returns(mockLibrary);


            _mockFileSystem.Setup(fs => fs.DirectoryExists(libraryPath)).Returns(false); // Simulate directory not existing initially
            _mockFileSystem.Setup(fs => fs.CreateDirectory(libraryPath)); // Expect it to be called
            _mockFileSystem.Setup(fs => fs.FileExists(fullExpectedPath)).Returns(false);


            var formFile = CreateTestFormFile(fileName, "some content");

            // Act
            var result = await _libraryManager.AddUploadedMediaFile(formFile, libraryId, null);

            // Assert
            Assert.True(result);
            _mockFileSystem.Verify(fs => fs.CreateDirectory(libraryPath), Times.Once);
            _mockLibraryMonitor.Verify(lm => lm.ReportFileSystemChange(fullExpectedPath, false), Times.Once);
        }
    }
}
