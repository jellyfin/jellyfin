using System;
using System.Linq;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.XbmcMetadata.Parsers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.XbmcMetadata.Tests.Parsers
{
    public class MusicVideoNfoParserTests
    {
        private readonly MovieNfoParser _parser;

        public MusicVideoNfoParserTests()
        {
            var providerManager = new Mock<IProviderManager>();
            providerManager.Setup(x => x.GetExternalIdInfos(It.IsAny<IHasProviderIds>()))
                .Returns(Enumerable.Empty<ExternalIdInfo>());
            var config = new Mock<IConfigurationManager>();
            config.Setup(x => x.GetConfiguration(It.IsAny<string>()))
                .Returns(new XbmcMetadataOptions());

            var user = new Mock<IUserManager>();
            var userData = new Mock<IUserDataManager>();
            var directoryService = new Mock<IDirectoryService>();

            _parser = new MovieNfoParser(
                new NullLogger<BaseNfoParser<MusicVideo>>(),
                config.Object,
                providerManager.Object,
                user.Object,
                userData.Object,
                directoryService.Object);
        }

        [Fact]
        public void Fetch_Valid_Succes()
        {
            var result = new MetadataResult<Video>()
            {
                Item = new MusicVideo()
            };

            _parser.Fetch(result, "Test Data/Dancing Queen.nfo", CancellationToken.None);
            var item = (MusicVideo)result.Item;

            Assert.Equal("Dancing Queen", item.Name);
            Assert.Single(item.Artists);
            Assert.Contains("ABBA", item.Artists);
            Assert.Equal("Arrival", item.Album);
        }

        [Fact]
        public void Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<Video>();

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, "Test Data/Dancing Queen.nfo", CancellationToken.None));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<Video>()
            {
                Item = new MusicVideo()
            };

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, string.Empty, CancellationToken.None));
        }
    }
}
