#pragma warning disable CA5369

using System;
using System.IO;
using System.Xml.Serialization;
using Jellyfin.NfoMetadata.Models;
using Jellyfin.NfoMetadata.Providers;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.NfoMetadata.Tests.Parsers
{
    public class MusicAlbumNfoProviderTests
    {
        private readonly XmlSerializer _serializer;
        private readonly AlbumNfoProvider _albumNfoProvider;

        public MusicAlbumNfoProviderTests()
        {
            _serializer = new XmlSerializer(typeof(AlbumNfo));
            _albumNfoProvider = new AlbumNfoProvider(new NullLogger<BaseNfoProvider<MusicAlbum, AlbumNfo>>(), null!, null!);
        }

        [Fact]
        public void Fetch_Valid_Succes()
        {
            var result = new MetadataResult<MusicAlbum>()
            {
                Item = new MusicAlbum()
            };

            using var stream = File.OpenRead("Test Data/The Best of 1980-1990.nfo");
            var nfo = _serializer.Deserialize(stream) as AlbumNfo;
            _albumNfoProvider.MapNfoToJellyfinObject(nfo, result);

            var item = result.Item;

            Assert.Equal("The Best of 1980-1990", item.Name);
            Assert.Equal(-1, item.CommunityRating);
            Assert.Equal(1989, item.ProductionYear);
            Assert.Contains("Pop", item.Genres);
            Assert.Single(item.Genres);
            Assert.Contains("Rock/Pop", item.Tags);
            Assert.Equal("The Best of 1980-1990 is the first greatest hits compilation by Irish rock band U2, released in November 1998. It mostly contains the group's hit singles from the eighties but also mixes in some live staples as well as one new recording, Sweetest Thing. In April 1999, a companion video (featuring music videos and live footage) was released. The album was followed by another compilation, The Best of 1990-2000, in 2002.\nA limited edition version containing a special B-sides disc was released on the same date as the single-disc version. At the time of release, the official word was that the 2-disc album would be available the first week the album went on sale, then pulled from the stores. While this threat never materialized, it did result in the 2-disc version being in very high demand. Both versions charted in the Billboard 200.\nThe boy on the cover is Peter Rowan, brother of Bono's friend Guggi (real name Derek Rowan) of the Virgin Prunes. He also appears on the covers of the early EP Three, two of the band's first three albums (Boy and War), and Early Demos.", item.Overview);

            Assert.Equal("6c301dbd-6ccb-3403-a6c4-6a22240a0297", item.ProviderIds[MetadataProvider.MusicBrainzReleaseGroup.ToString()]);
            Assert.Equal("59b5a40b-e2fd-3f18-a218-e8c9aae12ab5", item.ProviderIds[MetadataProvider.MusicBrainzAlbum.ToString()]);
        }

        [Fact]
        public void Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<MusicAlbum>();

            using var stream = File.OpenRead("Test Data/The Best of 1980-1990.nfo");
            var nfo = _serializer.Deserialize(stream) as AlbumNfo;

            Assert.Throws<ArgumentException>(() => _albumNfoProvider.MapNfoToJellyfinObject(nfo, result));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<MusicAlbum>()
            {
                Item = new MusicAlbum()
            };

            Assert.Throws<ArgumentException>(() => _albumNfoProvider.MapNfoToJellyfinObject(null, result));
        }
    }
}
