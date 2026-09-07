using Emby.Naming.Common;
using Emby.Server.Implementations.Library.Resolvers.TV;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class SeriesFolderVideoDetectionTests
    {
        private static readonly NamingOptions _namingOptions = new();

        [Fact]
        public void SeriesResolver_FolderWithEpisodeFiles_ResolvesToSeries()
        {
            var resolver = new SeriesResolver(Mock.Of<ILogger<SeriesResolver>>(), _namingOptions);
            var args = new ItemResolveArgs(
                Mock.Of<IServerApplicationPaths>(),
                null)
            {
                Parent = new Folder(),
                CollectionType = null,
                FileInfo = new FileSystemMetadata
                {
                    FullName = "/media/Starlight.S02.1080p.NOVA.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HUN.ENG-Z9R",
                    IsDirectory = true
                },
                FileSystemChildren = new[]
                {
                    new FileSystemMetadata
                    {
                        FullName = "/media/Starlight.S02.1080p.NOVA.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HUN.ENG-Z9R/Starlight.S02E01.1080p.NOVA.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HUN.ENG-Z9R.mkv",
                        Name = "Starlight.S02E01.1080p.NOVA.WEB-DL.DDP5.1.Atmos.DV.HDR.H265.HUN.ENG-Z9R.mkv"
                    }
                }
            };

            var result = resolver.ResolvePath(args);

            Assert.IsType<Series>(result);
        }
    }
}
