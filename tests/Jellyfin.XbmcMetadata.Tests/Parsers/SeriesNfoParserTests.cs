#pragma warning disable CA5369

using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Jellyfin.NfoMetadata.Models;
using Jellyfin.NfoMetadata.Providers;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.XbmcMetadata.Parsers.Tests
{
    public class SeriesNfoParserTests
    {
        private readonly XmlSerializer _serializer;
        private readonly SeriesNfoProvider _seriesNfoProvider;

        public SeriesNfoParserTests()
        {
            var providerManager = new Mock<IProviderManager>();
            providerManager.Setup(x => x.GetExternalIdInfos(It.IsAny<IHasProviderIds>()))
                .Returns(Enumerable.Empty<ExternalIdInfo>());
            var config = new Mock<IConfigurationManager>();
            config.Setup(x => x.GetConfiguration(It.IsAny<string>()))
                .Returns(new XbmcMetadataOptions());

            _serializer = new XmlSerializer(typeof(SeriesNfo));
            _seriesNfoProvider = new SeriesNfoProvider(new NullLogger<SeriesNfoProvider>(), null!, null!);
        }

        [Fact]
        public void Fetch_Valid_Succes()
        {
            var result = new MetadataResult<Series>()
            {
                Item = new Series()
            };

            using var stream = File.OpenRead("Test Data/American Gods.nfo");
            var nfo = _serializer.Deserialize(stream) as SeriesNfo;
            _seriesNfoProvider.MapNfoToJellyfinObject(nfo, result);

            var item = result.Item;

            Assert.Equal("American Gods", item.OriginalTitle);
            Assert.Equal(string.Empty, item.Tagline);
            Assert.Equal(0, item.RunTimeTicks);
            Assert.Equal("46639", item.ProviderIds["tmdb"]);
            Assert.Equal("253573", item.ProviderIds["tvdb"]);

            Assert.Equal(3, item.Genres.Length);
            Assert.Contains("Drama", item.Genres);
            Assert.Contains("Mystery", item.Genres);
            Assert.Contains("Sci-Fi & Fantasy", item.Genres);

            Assert.Equal(new DateTime(2017, 4, 30), item.PremiereDate);
            Assert.Single(item.Studios);
            Assert.Contains("Starz", item.Studios);

            Assert.Equal(6, result.People.Count);

            Assert.True(result.People.All(x => x.Type == PersonType.Actor));

            // Only test one actor
            var sweeney = result.People.FirstOrDefault(x => x.Role.Equals("Mad Sweeney", StringComparison.Ordinal));
            Assert.NotNull(sweeney);
            Assert.Equal("Pablo Schreiber", sweeney!.Name);
            Assert.Equal(3, sweeney!.SortOrder);
            Assert.Equal("http://image.tmdb.org/t/p/original/uo8YljeePz3pbj7gvWXdB4gOOW4.jpg", sweeney!.ImageUrl);

            Assert.Equal(new DateTime(2017, 10, 7, 14, 25, 47), item.DateCreated);
        }

        [Fact]
        public void Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<Series>();

            using var stream = File.OpenRead("Test Data/American Gods.nfo");
            var nfo = _serializer.Deserialize(stream) as SeriesNfo;

            Assert.Throws<ArgumentException>(() => _seriesNfoProvider.MapNfoToJellyfinObject(nfo, result));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<Series>()
            {
                Item = new Series()
            };

            Assert.Throws<ArgumentException>(() => _seriesNfoProvider.MapNfoToJellyfinObject(null, result));
        }
    }
}
