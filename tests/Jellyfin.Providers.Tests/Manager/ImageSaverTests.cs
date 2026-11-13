using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private readonly string _dir;

        public ImageSaverTests()
        {
            _dir = Directory.CreateTempSubdirectory("jellyfin-imagesaver").FullName;
        }

        public void Dispose() => Directory.Delete(_dir, true);

        [Fact]
        public async Task SaveImage_ReplaceNumberedBackdrop_KeepsFileName()
        {
            var config = new Mock<IServerConfigurationManager>();
            config.Setup(c => c.Configuration).Returns(new ServerConfiguration());
            config.Setup(c => c.ApplicationPaths.InternalMetadataPath).Returns(Path.Combine(_dir, "metadata"));
            BaseItem.ConfigurationManager = config.Object;
            BaseItem.MediaSourceManager = Mock.Of<IMediaSourceManager>();
            BaseItem.FileSystem = Mock.Of<IFileSystem>();

            var item = new LocalItem { Id = Guid.NewGuid(), Path = Path.Combine(_dir, "movie.mkv") };
            item.AddImage(new ItemImageInfo { Path = Path.Combine(_dir, "backdrop.jpg"), Type = ImageType.Backdrop });
            item.AddImage(new ItemImageInfo { Path = Path.Combine(_dir, "backdrop1.jpg"), Type = ImageType.Backdrop });

            var fileSystem = new Mock<IFileSystem>();
            fileSystem.Setup(f => f.GetFileInfo(It.IsAny<string>())).Returns((string path) => new FileSystemMetadata { FullName = path });

            var saver = new ImageSaver(config.Object, Mock.Of<ILibraryMonitor>(), fileSystem.Object, NullLogger.Instance);
            await saver.SaveImage(item, new MemoryStream([1]), "image/jpeg", ImageType.Backdrop, 1, TestContext.Current.CancellationToken);

            Assert.Equal(Path.Combine(_dir, "backdrop1.jpg"), item.GetImages(ImageType.Backdrop).ElementAt(1).Path);
            fileSystem.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Never);
        }

        private sealed class LocalItem : BaseItem
        {
            public override bool SupportsLocalMetadata => true;

            public override bool IsSaveLocalMetadataEnabled() => true;
        }
    }
}
