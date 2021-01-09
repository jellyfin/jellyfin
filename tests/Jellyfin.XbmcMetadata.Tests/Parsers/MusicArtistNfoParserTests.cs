using System;
using System.Linq;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.XbmcMetadata.Parsers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.XbmcMetadata.Parsers.Tests
{
    public class MusicArtistNfoParserTests
    {
        private readonly BaseNfoParser<MusicArtist> _parser;

        public MusicArtistNfoParserTests()
        {
            var providerManager = new Mock<IProviderManager>();
            providerManager.Setup(x => x.GetExternalIdInfos(It.IsAny<IHasProviderIds>()))
                .Returns(Enumerable.Empty<ExternalIdInfo>());
            var config = new Mock<IConfigurationManager>();
            config.Setup(x => x.GetConfiguration(It.IsAny<string>()))
                .Returns(new XbmcMetadataOptions());
            _parser = new BaseNfoParser<MusicArtist>(new NullLogger<BaseNfoParser<MusicArtist>>(), config.Object, providerManager.Object);
        }

        [Fact]
        public void Fetch_Valid_Succes()
        {
            var result = new MetadataResult<MusicArtist>()
            {
                Item = new MusicArtist()
            };

            _parser.Fetch(result, "Test Data/U2.nfo", CancellationToken.None);
            var item = result.Item;

            Assert.Equal("U2", item.Name);
            Assert.Equal("U2", item.SortName);
            Assert.Equal("a3cb23fc-acd3-4ce0-8f36-1e5aa6a18432", item.ProviderIds[MetadataProvider.MusicBrainzArtist.ToString()]);

            Assert.Single(item.Genres);
            Assert.Equal("Rock", item.Genres[0]);
        }

        [Fact]
        public void Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<MusicArtist>();

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, "Test Data/U2.nfo", CancellationToken.None));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<MusicArtist>()
            {
                Item = new MusicArtist()
            };

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, string.Empty, CancellationToken.None));
        }
    }
}
