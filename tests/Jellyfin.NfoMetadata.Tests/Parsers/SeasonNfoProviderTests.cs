#pragma warning disable CA5369

using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Jellyfin.NfoMetadata.Models;
using Jellyfin.NfoMetadata.Providers;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.NfoMetadata.Tests.Parsers
{
    public class SeasonNfoProviderTests
    {
        private readonly XmlSerializer _serializer;
        private readonly SeasonNfoProvider _seasonNfoProvider;

        public SeasonNfoProviderTests()
        {
            _serializer = new XmlSerializer(typeof(SeasonNfo));
            _seasonNfoProvider = new SeasonNfoProvider(new NullLogger<SeasonNfoProvider>(), null!, null!);
        }

        [Fact]
        public void Fetch_Valid_Succes()
        {
            var result = new MetadataResult<Season>()
            {
                Item = new Season()
            };

            using var stream = File.OpenRead("Test Data/Season 01.nfo");
            var nfo = _serializer.Deserialize(stream) as SeasonNfo;
            _seasonNfoProvider.MapNfoToJellyfinObject(nfo, result);

            var item = result.Item;

            Assert.Equal("Season 1", item.Name);
            Assert.Equal(1, item.IndexNumber);
            Assert.False(item.IsLocked);
            Assert.Equal(2019, item.ProductionYear);
            Assert.Equal(new DateTime(2019, 11, 08), item.PremiereDate);
            Assert.Equal(new DateTime(2020, 06, 14, 17, 26, 51), item.DateCreated);
            Assert.Equal("359728", item.GetProviderId(MetadataProvider.Tvdb));

            Assert.Equal(10, result.People.Count);

            Assert.True(result.People.All(x => x.Type == PersonType.Actor));

            // Only test one actor
            var nini = result.People.FirstOrDefault(x => x.Role.Equals("Nini", StringComparison.Ordinal));
            Assert.NotNull(nini);
            Assert.Equal("Olivia Rodrigo", nini!.Name);
            Assert.Equal(0, nini!.SortOrder);
            Assert.Equal("/config/metadata/People/O/Olivia Rodrigo/poster.jpg", nini!.ImageUrl);
        }

        [Fact]
        public void Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<Season>();

            using var stream = File.OpenRead("Test Data/Season 01.nfo");
            var nfo = _serializer.Deserialize(stream) as SeasonNfo;

            Assert.Throws<ArgumentException>(() => _seasonNfoProvider.MapNfoToJellyfinObject(nfo, result));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<Season>()
            {
                Item = new Season()
            };

            Assert.Throws<ArgumentException>(() => _seasonNfoProvider.MapNfoToJellyfinObject(null, result));
        }
    }
}
