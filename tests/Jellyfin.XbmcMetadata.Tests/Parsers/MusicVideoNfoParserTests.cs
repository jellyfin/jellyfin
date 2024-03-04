using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations;
using Jellyfin.Server.Implementations.Library.Interfaces;
using Jellyfin.Server.Implementations.Library.Managers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.XbmcMetadata.Parsers;
using Microsoft.EntityFrameworkCore;
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

            // Arrange
            var user = new Mock<IUserManager>();
            var userData = new Mock<IUserDataManager>();
            var dbContextFactory = new InMemoryDbContextFactory();
            var genreManager = new GenreManager(dbContextFactory);
            var directoryService = new Mock<IDirectoryService>();

            _parser = new MovieNfoParser(
                new NullLogger<BaseNfoParser<MusicVideo>>(),
                config.Object,
                providerManager.Object,
                user.Object,
                userData.Object,
                genreManager,
                directoryService.Object);
        }

        [Fact]
        public async Task Fetch_Valid_Success()
        {
            var result = new MetadataResult<Video>()
            {
                Item = new MusicVideo()
            };

            await _parser.Fetch(result, "Test Data/Dancing Queen.nfo", CancellationToken.None);
            var item = (MusicVideo)result.Item;

            Assert.Equal("Dancing Queen", item.Name);
            Assert.Single(item.Artists);
            Assert.Contains("ABBA", item.Artists);
            Assert.Equal("Arrival", item.Album);
        }

        [Fact]
        public async Task Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<Video>();

            await Assert.ThrowsAsync<ArgumentException>(() => _parser.Fetch(result, "Test Data/Dancing Queen.nfo", CancellationToken.None));
        }

        [Fact]
        public async Task Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<Video>()
            {
                Item = new MusicVideo()
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _parser.Fetch(result, string.Empty, CancellationToken.None));
        }
    }
}
