using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Library.Interfaces;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.MusicBrainz;
using MediaBrowser.XbmcMetadata.Parsers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.XbmcMetadata.Tests.Parsers
{
    public class MusicArtistNfoParserTests
    {
        private readonly BaseNfoParser<MusicArtist> _parser;

        public MusicArtistNfoParserTests()
        {
            var providerManager = new Mock<IProviderManager>();

            var musicBrainzArtist = new MusicBrainzArtistExternalId();
            var externalIdInfo = new ExternalIdInfo(musicBrainzArtist.ProviderName, musicBrainzArtist.Key, musicBrainzArtist.Type, "MusicBrainzServer");

            providerManager.Setup(x => x.GetExternalIdInfos(It.IsAny<IHasProviderIds>()))
                .Returns(new[] { externalIdInfo });

            var config = new Mock<IConfigurationManager>();
            config.Setup(x => x.GetConfiguration(It.IsAny<string>()))
                .Returns(new XbmcMetadataOptions());
            var user = new Mock<IUserManager>();
            var userData = new Mock<IUserDataManager>();
            var genreManager = new Mock<IGenreManager>();
            var directoryService = new Mock<IDirectoryService>();

            _parser = new BaseNfoParser<MusicArtist>(
                new NullLogger<BaseNfoParser<MusicArtist>>(),
                config.Object,
                providerManager.Object,
                user.Object,
                userData.Object,
                genreManager.Object,
                directoryService.Object);
        }

        [Fact]
        public async Task Fetch_Valid_Success()
        {
            var result = new MetadataResult<MusicArtist>()
            {
                Item = new MusicArtist()
            };

            await _parser.Fetch(result, "Test Data/U2.nfo", CancellationToken.None);
            var item = result.Item;

            Assert.Equal("U2", item.Name);
            Assert.Equal("U2", item.SortName);
            Assert.Equal("a3cb23fc-acd3-4ce0-8f36-1e5aa6a18432", item.ProviderIds[MetadataProvider.MusicBrainzArtist.ToString()]);

            Assert.Single(item.Genres);
            Assert.Equal("Rock", item.Genres[0]);
        }

        [Fact]
        public async Task Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<MusicArtist>();

            await Assert.ThrowsAsync<ArgumentException>(() => _parser.Fetch(result, "Test Data/U2.nfo", CancellationToken.None));
        }

        [Fact]
        public async Task Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<MusicArtist>()
            {
                Item = new MusicArtist()
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _parser.Fetch(result, string.Empty, CancellationToken.None));
        }
    }
}
