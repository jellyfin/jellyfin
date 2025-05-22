using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public class MediaUploadControllerTests
    {
        private readonly Mock<ILogger<MediaUploadController>> _mockLogger;
        private readonly Mock<ILibraryManager> _mockLibraryManager;
        private readonly Mock<IUserManager> _mockUserManager;
        private readonly MediaUploadController _mediaUploadController;

        public MediaUploadControllerTests()
        {
            _mockLogger = new Mock<ILogger<MediaUploadController>>();
            _mockLibraryManager = new Mock<ILibraryManager>();
            _mockUserManager = new Mock<IUserManager>(); // Though not used in current controller logic, good to have for completeness

            _mediaUploadController = new MediaUploadController(
                _mockLogger.Object,
                _mockLibraryManager.Object,
                _mockUserManager.Object);
        }

        private IFormFile CreateTestFormFile(string fileName, string content)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            return new FormFile(stream, 0, stream.Length, "file", fileName);
        }

        [Fact]
        public async Task UploadMediaFile_ValidFileAndParams_ReturnsNoContent()
        {
            // Arrange
            var mockFile = CreateTestFormFile("test.mp4", "dummy video content");
            var libraryId = Guid.NewGuid();
            var collectionType = "movies";

            _mockLibraryManager
                .Setup(lm => lm.AddUploadedMediaFile(mockFile, libraryId, collectionType))
                .ReturnsAsync(true);

            // Act
            var result = await _mediaUploadController.UploadMediaFile(mockFile, libraryId, collectionType);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLibraryManager.Verify(lm => lm.AddUploadedMediaFile(mockFile, libraryId, collectionType), Times.Once);
        }

        [Fact]
        public async Task UploadMediaFile_NullFile_ReturnsBadRequest()
        {
            // Arrange
            // Act
            var result = await _mediaUploadController.UploadMediaFile(null!, null, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("File is required and cannot be empty.", badRequestResult.Value);
        }

        [Fact]
        public async Task UploadMediaFile_EmptyFile_ReturnsBadRequest()
        {
            // Arrange
            var mockFile = CreateTestFormFile("empty.txt", string.Empty);
            // Set the length to 0 by creating a new FormFile with an empty stream
            var emptyStream = new MemoryStream();
            var emptyFile = new FormFile(emptyStream, 0, 0, "file", "empty.txt");


            // Act
            var result = await _mediaUploadController.UploadMediaFile(emptyFile, null, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("File is required and cannot be empty.", badRequestResult.Value);
        }

        [Fact]
        public async Task UploadMediaFile_LibraryManagerReturnsFalse_ReturnsInternalServerError()
        {
            // Arrange
            var mockFile = CreateTestFormFile("test.mp4", "dummy video content");
            _mockLibraryManager
                .Setup(lm => lm.AddUploadedMediaFile(mockFile, null, null))
                .ReturnsAsync(false);

            // Act
            var result = await _mediaUploadController.UploadMediaFile(mockFile, null, null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal($"Failed to save file {mockFile.FileName}. The server encountered an internal error.", statusCodeResult.Value);
        }

        [Fact]
        public async Task UploadMediaFile_LibraryManagerThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var mockFile = CreateTestFormFile("test.mp4", "dummy video content");
            var exceptionMessage = "Test exception from LibraryManager";
            _mockLibraryManager
                .Setup(lm => lm.AddUploadedMediaFile(mockFile, null, null))
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            var result = await _mediaUploadController.UploadMediaFile(mockFile, null, null);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred during file upload.", statusCodeResult.Value);
        }

        [Fact]
        public async Task UploadMediaFile_ValidFileWithoutLibraryId_ReturnsNoContent()
        {
            // Arrange
            var mockFile = CreateTestFormFile("test.mkv", "more dummy content");
            string? collectionType = "tvshows";

            _mockLibraryManager
                .Setup(lm => lm.AddUploadedMediaFile(mockFile, null, collectionType))
                .ReturnsAsync(true);

            // Act
            var result = await _mediaUploadController.UploadMediaFile(mockFile, null, collectionType);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLibraryManager.Verify(lm => lm.AddUploadedMediaFile(mockFile, null, collectionType), Times.Once);
        }

        [Fact]
        public async Task UploadMediaFile_ValidFileWithoutCollectionType_ReturnsNoContent()
        {
            // Arrange
            var mockFile = CreateTestFormFile("test.mp3", "audio data");
            Guid? libraryId = Guid.NewGuid();

            _mockLibraryManager
                .Setup(lm => lm.AddUploadedMediaFile(mockFile, libraryId, null))
                .ReturnsAsync(true);

            // Act
            var result = await _mediaUploadController.UploadMediaFile(mockFile, libraryId, null);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLibraryManager.Verify(lm => lm.AddUploadedMediaFile(mockFile, libraryId, null), Times.Once);
        }

        [Fact]
        public async Task UploadMediaFile_ValidFileWithoutOptionalParams_ReturnsNoContent()
        {
            // Arrange
            var mockFile = CreateTestFormFile("generic.file", "some data");

            _mockLibraryManager
                .Setup(lm => lm.AddUploadedMediaFile(mockFile, null, null))
                .ReturnsAsync(true);

            // Act
            var result = await _mediaUploadController.UploadMediaFile(mockFile, null, null);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockLibraryManager.Verify(lm => lm.AddUploadedMediaFile(mockFile, null, null), Times.Once);
        }
    }
}
