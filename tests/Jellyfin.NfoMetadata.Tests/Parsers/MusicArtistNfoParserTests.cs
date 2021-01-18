#pragma warning disable CA5369

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Jellyfin.NfoMetadata.Models;
using Jellyfin.NfoMetadata.Providers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.NfoMetadata.Tests.Parsers
{
    public class MusicArtistNfoParserTests
    {
        private readonly XmlSerializer _serializer;
        private readonly ArtistNfoProvider _artistNfoProvider;

        public MusicArtistNfoParserTests()
        {
            var providerManager = new Mock<IProviderManager>();
            providerManager.Setup(x => x.GetExternalIdInfos(It.IsAny<IHasProviderIds>()))
                .Returns(Enumerable.Empty<ExternalIdInfo>());
            var config = new Mock<IConfigurationManager>();
            config.Setup(x => x.GetConfiguration(It.IsAny<string>()))
                .Returns(new XbmcMetadataOptions());

            _serializer = new XmlSerializer(typeof(ArtistNfo));
            _artistNfoProvider = new ArtistNfoProvider(new NullLogger<BaseNfoProvider<MusicArtist, ArtistNfo>>(), null!, null!);
        }

        [Fact]
        public void Fetch_Valid_Succes()
        {
            var result = new MetadataResult<MusicArtist>()
            {
                Item = new MusicArtist()
            };

            using var stream = File.OpenRead("Test Data/U2.nfo");
            var nfo = _serializer.Deserialize(stream) as ArtistNfo;
            _artistNfoProvider.MapNfoToJellyfinObject(nfo, result);

            var item = result.Item;

            Assert.Equal("U2", item.Name);
            Assert.Equal("U2", item.SortName);
            // Assert.Equal("a3cb23fc-acd3-4ce0-8f36-1e5aa6a18432", item.ProviderIds[MetadataProvider.MusicBrainzArtist.ToString()]); // todo

            Assert.Single(item.Genres);
            Assert.Equal("Rock", item.Genres[0]);
        }

        [Fact]
        public void Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<MusicArtist>();

            using var stream = File.OpenRead("Test Data/U2.nfo");
            var nfo = _serializer.Deserialize(stream) as ArtistNfo;

            Assert.Throws<ArgumentException>(() => _artistNfoProvider.MapNfoToJellyfinObject(nfo, result));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<MusicArtist>()
            {
                Item = new MusicArtist()
            };

            Assert.Throws<ArgumentException>(() => _artistNfoProvider.MapNfoToJellyfinObject(null, result));
        }
    }
}
