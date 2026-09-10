using Emby.Naming.Common;
using Emby.Naming.Video;
using Emby.Server.Implementations.Library.Resolvers.Movies;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class MovieSampleFolderTests
    {
        private static readonly NamingOptions _namingOptions = new();
        private static readonly VideoListResolver _videoListResolver = new(_namingOptions);

        [Fact]
        public void MovieResolver_MovieFolderWithSampleSubfolder_ResolvesToMovie()
        {
            var libraryManager = new Mock<ILibraryManager>();
            libraryManager.Setup(m => m.GetLibraryOptions(It.IsAny<BaseItem>())).Returns(new LibraryOptions());
            libraryManager.Setup(m => m.IgnoreFile(It.IsAny<FileSystemMetadata>(), It.IsAny<BaseItem>())).Returns(false);

            var resolver = new MovieResolver(Mock.Of<IImageProcessor>(), Mock.Of<ILogger<MovieResolver>>(), _namingOptions, Mock.Of<IDirectoryService>(), _videoListResolver);
            var args = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                libraryManager.Object)
            {
                Parent = new Folder(),
                CollectionType = CollectionType.movies,
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/30.Years.After.2026.1080p.MA.WEBRip.DDP5.1.Atmos.x264.HUN-PULSAR",
                    IsDirectory = true
                },
                FileSystemChildren = new[]
                {
                    new FileSystemMetadata
                    {
                        FullName = "/media/30.Years.After.2026.1080p.MA.WEBRip.DDP5.1.Atmos.x264.HUN-PULSAR/quasar-30.years.after.2026.1080p.webrip.ma.mkv",
                        Name = "quasar-30.years.after.2026.1080p.webrip.ma.mkv"
                    },
                    new FileSystemMetadata
                    {
                        FullName = "/media/30.Years.After.2026.1080p.MA.WEBRip.DDP5.1.Atmos.x264.HUN-PULSAR/Sample",
                        Name = "Sample",
                        IsDirectory = true
                    }
                }
            };

            var result = resolver.ResolvePath(args);

            Assert.IsType<Movie>(result);
        }
    }
}
