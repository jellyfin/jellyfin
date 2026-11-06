using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.Manager
{
    public sealed class ImageSaverTests : IDisposable
    {
        private readonly string _metadataPath = Path.Combine(Path.GetTempPath(), "jellyfin-image-saver-" + Guid.NewGuid().ToString("N"));

        public ImageSaverTests()
        {
            Directory.CreateDirectory(_metadataPath);
        }

        public void Dispose()
        {
            Directory.Delete(_metadataPath, true);
        }

        [Fact]
        public async Task SaveImage_PreviousImageCannotBeDeleted_LeavesItemAndFilesUnchanged()
        {
            var oldImagePath = Path.Combine(_metadataPath, "old.jpg");
            var newImagePath = Path.Combine(_metadataPath, "poster.png");

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(f => f.FileExists(It.IsAny<string>())).Returns<string>(File.Exists);
            fileSystem.Setup(f => f.GetFileInfo(It.IsAny<string>())).Returns<string>(p => new FileSystemMetadata { FullName = p });
            fileSystem.Setup(f => f.DeleteFile(oldImagePath)).Throws(new IOException("Read-only file system"));
            fileSystem.Setup(f => f.DeleteFile(newImagePath)).Callback<string>(File.Delete);
            BaseItem.FileSystem ??= Mock.Of<IFileSystem>();

            var applicationPaths = new Mock<IServerApplicationPaths>();
            applicationPaths.Setup(p => p.InternalMetadataPath).Returns(_metadataPath);
            var config = new Mock<IServerConfigurationManager>();
            config.Setup(c => c.ApplicationPaths).Returns(applicationPaths.Object);
            config.Setup(c => c.Configuration).Returns(new ServerConfiguration());

            var item = new Mock<Photo> { CallBase = true };
            item.Setup(m => m.GetInternalMetadataPath()).Returns(_metadataPath);
            item.Object.SetImagePath(ImageType.Primary, 0, new FileSystemMetadata { FullName = oldImagePath });

            var imageSaver = new ImageSaver(config.Object, Mock.Of<ILibraryMonitor>(), fileSystem.Object, NullLogger.Instance);

            await Assert.ThrowsAsync<IOException>(() => imageSaver.SaveImage(item.Object, new MemoryStream(new byte[] { 1, 2, 3 }), "image/png", ImageType.Primary, null, CancellationToken.None));

            Assert.Equal(oldImagePath, item.Object.GetImageInfo(ImageType.Primary, 0).Path);
            Assert.False(File.Exists(newImagePath));
        }
    }
}
