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

#pragma warning disable CA5369

namespace Jellyfin.NfoMetadata.Tests.Parsers
{
    public class EpisodeNfoParserTests
    {
        private readonly XmlSerializer _serializer;
        private readonly EpisodeNfoProvider _episodeNfoProvider;

        public EpisodeNfoParserTests()
        {
            var providerManager = new Mock<IProviderManager>();
            providerManager.Setup(x => x.GetExternalIdInfos(It.IsAny<IHasProviderIds>()))
                .Returns(Enumerable.Empty<ExternalIdInfo>());
            var config = new Mock<IConfigurationManager>();
            config.Setup(x => x.GetConfiguration(It.IsAny<string>()))
                .Returns(new XbmcMetadataOptions());

            _serializer = new XmlSerializer(typeof(EpisodeNfo));
            _episodeNfoProvider = new EpisodeNfoProvider(new NullLogger<BaseNfoProvider<Episode, EpisodeNfo>>(), null!, null!);
        }

        [Fact]
        public void Fetch_Valid_Succes()
        {
            var result = new MetadataResult<Episode>()
            {
                Item = new Episode()
            };

            using var stream = File.OpenRead("Test Data/The Bone Orchard.nfo");
            var nfo = _serializer.Deserialize(stream) as EpisodeNfo;
            _episodeNfoProvider.MapNfoToJellyfinObject(nfo, result);

            var item = result.Item;
            Assert.Equal("The Bone Orchard", item.Name);
            Assert.Equal("American Gods", item.SeriesName);
            Assert.Equal(1, item.IndexNumber);
            Assert.Equal(1, item.ParentIndexNumber);
            Assert.Equal("When Shadow Moon is released from prison early after the death of his wife, he meets Mr. Wednesday and is recruited as his bodyguard. Shadow discovers that this may be more than he bargained for.", item.Overview);
            Assert.Equal(0, item.RunTimeTicks);
            Assert.Equal("16", item.OfficialRating);
            Assert.Contains("Drama", item.Genres);
            Assert.Contains("Mystery", item.Genres);
            Assert.Contains("Sci-Fi & Fantasy", item.Genres);
            Assert.Equal(new DateTime(2017, 4, 30), item.PremiereDate);
            Assert.Equal(2017, item.ProductionYear);
            Assert.Single(item.Studios);
            Assert.Contains("Starz", item.Studios);

            // Credits
            var writers = result.People.Where(x => x.Type == PersonType.Writer).ToArray();
            Assert.Equal(2, writers.Length);
            Assert.Contains("Bryan Fuller", writers.Select(x => x.Name));
            Assert.Contains("Michael Green", writers.Select(x => x.Name));

            // Direcotrs
            var directors = result.People.Where(x => x.Type == PersonType.Director).ToArray();
            Assert.Single(directors);
            Assert.Contains("David Slade", directors.Select(x => x.Name));

            // Actors
            var actors = result.People.Where(x => x.Type == PersonType.Actor).ToArray();
            Assert.Equal(11, actors.Length);
            // Only test one actor
            var shadow = actors.FirstOrDefault(x => x.Role.Equals("Shadow Moon", StringComparison.Ordinal));
            Assert.NotNull(shadow);
            Assert.Equal("Ricky Whittle", shadow!.Name);
            Assert.Equal(0, shadow!.SortOrder);
            Assert.Equal("http://image.tmdb.org/t/p/original/cjeDbVfBp6Qvb3C74Dfy7BKDTQN.jpg", shadow!.ImageUrl);

            Assert.Equal(new DateTime(2017, 10, 7, 14, 25, 47), item.DateCreated);
        }

        [Fact]
        public void Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<Episode>();

            using var stream = File.OpenRead("Test Data/The Bone Orchard.nfo");
            var nfo = _serializer.Deserialize(stream) as EpisodeNfo;

            Assert.Throws<ArgumentException>(() => _episodeNfoProvider.MapNfoToJellyfinObject(nfo, result));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<Episode>()
            {
                Item = new Episode()
            };

            Assert.Throws<ArgumentException>(() => _episodeNfoProvider.MapNfoToJellyfinObject(null, result));
        }
    }
}
