using System;
using System.Threading;
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
    public class MusicAlbumNfoProviderTests
    {
        private readonly BaseNfoParser<MusicAlbum> _parser;

        public MusicAlbumNfoProviderTests()
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
            var directoryService = new Mock<IDirectoryService>();

            _parser = new BaseNfoParser<MusicAlbum>(
                new NullLogger<BaseNfoParser<MusicAlbum>>(),
                config.Object,
                providerManager.Object,
                user.Object,
                userData.Object,
                directoryService.Object);
        }

        [Fact]
        public void Fetch_Valid_Success()
        {
            var result = new MetadataResult<MusicAlbum>()
            {
                Item = new MusicAlbum()
            };

            _parser.Fetch(result, "Test Data/The Best of 1980-1990.nfo", CancellationToken.None);
            var item = result.Item;

            Assert.Equal("The Best of 1980-1990", item.Name);
            Assert.Equal(1989, item.ProductionYear);
            Assert.Contains("Pop", item.Genres);
            Assert.Single(item.Genres);
            Assert.Contains("Rock/Pop", item.Tags);
            Assert.Equal("The Best of 1980-1990 is the first greatest hits compilation by Irish rock band U2, released in November 1998. It mostly contains the group's hit singles from the eighties but also mixes in some live staples as well as one new recording, Sweetest Thing. In April 1999, a companion video (featuring music videos and live footage) was released. The album was followed by another compilation, The Best of 1990-2000, in 2002.\nA limited edition version containing a special B-sides disc was released on the same date as the single-disc version. At the time of release, the official word was that the 2-disc album would be available the first week the album went on sale, then pulled from the stores. While this threat never materialized, it did result in the 2-disc version being in very high demand. Both versions charted in the Billboard 200.\nThe boy on the cover is Peter Rowan, brother of Bono's friend Guggi (real name Derek Rowan) of the Virgin Prunes. He also appears on the covers of the early EP Three, two of the band's first three albums (Boy and War), and Early Demos.", item.Overview);
        }

        [Fact]
        public void Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<MusicAlbum>();

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, "Test Data/The Best of 1980-1990.nfo", CancellationToken.None));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<MusicAlbum>()
            {
                Item = new MusicAlbum()
            };

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, string.Empty, CancellationToken.None));
        }
    }
}
