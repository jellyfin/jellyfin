using System;
using System.IO;
using Jellyfin.Server.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Moq;
using Xunit;

namespace Jellyfin.Server.Tests
{
    public class SafeTimestampFileInfoTests
    {
        private static readonly DateTimeOffset ValidTimestamp = new DateTimeOffset(2024, 12, 31, 12, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset PreWin32Timestamp = new DateTimeOffset(1600, 1, 1, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

        [Fact]
        public void LastModified_WithValidTimestamp_ReturnsOriginal()
        {
            // Arrange
            var mockFileInfo = new Mock<IFileInfo>();
            mockFileInfo.Setup(f => f.LastModified).Returns(ValidTimestamp);

            var safeFileInfo = new SafeTimestampFileInfo(mockFileInfo.Object);

            // Act
            var result = safeFileInfo.LastModified;

            // Assert
            Assert.Equal(ValidTimestamp, result);
        }

        [Fact]
        public void LastModified_WithDateMinValue_ReturnsSafeFallback()
        {
            // Arrange
            var mockFileInfo = new Mock<IFileInfo>();
            mockFileInfo.Setup(f => f.LastModified).Returns(DateTimeOffset.MinValue);

            var safeFileInfo = new SafeTimestampFileInfo(mockFileInfo.Object);

            // Act
            var result = safeFileInfo.LastModified;

            // Assert
            Assert.Equal(UnixEpoch, result);
        }

        [Fact]
        public void LastModified_WithPre1601Timestamp_ReturnsSafeFallback()
        {
            // Arrange
            var mockFileInfo = new Mock<IFileInfo>();
            mockFileInfo.Setup(f => f.LastModified).Returns(PreWin32Timestamp);

            var safeFileInfo = new SafeTimestampFileInfo(mockFileInfo.Object);

            // Act
            var result = safeFileInfo.LastModified;

            // Assert
            Assert.Equal(UnixEpoch, result);
        }

        [Fact]
        public void LastModified_WithWin32EpochPlusOneDay_ReturnsOriginal()
        {
            // Arrange - exactly at the boundary (1601-01-02 should be valid)
            var boundaryTimestamp = new DateTimeOffset(1601, 1, 2, 0, 0, 0, TimeSpan.Zero);
            var mockFileInfo = new Mock<IFileInfo>();
            mockFileInfo.Setup(f => f.LastModified).Returns(boundaryTimestamp);

            var safeFileInfo = new SafeTimestampFileInfo(mockFileInfo.Object);

            // Act
            var result = safeFileInfo.LastModified;

            // Assert
            Assert.Equal(boundaryTimestamp, result);
        }

        [Fact]
        public void Properties_DelegateCorrectly()
        {
            // Arrange
            var mockFileInfo = new Mock<IFileInfo>();
            mockFileInfo.Setup(f => f.Exists).Returns(true);
            mockFileInfo.Setup(f => f.Length).Returns(12345);
            mockFileInfo.Setup(f => f.PhysicalPath).Returns("/path/to/file.txt");
            mockFileInfo.Setup(f => f.Name).Returns("file.txt");
            mockFileInfo.Setup(f => f.IsDirectory).Returns(false);
            mockFileInfo.Setup(f => f.LastModified).Returns(ValidTimestamp);

            var safeFileInfo = new SafeTimestampFileInfo(mockFileInfo.Object);

            // Act & Assert
            Assert.True(safeFileInfo.Exists);
            Assert.Equal(12345, safeFileInfo.Length);
            Assert.Equal("/path/to/file.txt", safeFileInfo.PhysicalPath);
            Assert.Equal("file.txt", safeFileInfo.Name);
            Assert.False(safeFileInfo.IsDirectory);
        }

        [Fact]
        public void CreateReadStream_DelegatesCorrectly()
        {
            // Arrange
            using var expectedStream = new MemoryStream();
            var mockFileInfo = new Mock<IFileInfo>();
            mockFileInfo.Setup(f => f.CreateReadStream()).Returns(expectedStream);
            mockFileInfo.Setup(f => f.LastModified).Returns(ValidTimestamp);

            var safeFileInfo = new SafeTimestampFileInfo(mockFileInfo.Object);

            // Act
            var result = safeFileInfo.CreateReadStream();

            // Assert
            Assert.Same(expectedStream, result);
        }

        [Fact]
        public void Constructor_WithNullInner_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SafeTimestampFileInfo(null!));
        }
    }
}
