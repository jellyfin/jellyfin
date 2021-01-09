using System;
using System.Linq;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
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
    public class MovieNfoParserTests
    {
        private readonly MovieNfoParser _parser;

        public MovieNfoParserTests()
        {
            var providerManager = new Mock<IProviderManager>();
            providerManager.Setup(x => x.GetExternalIdInfos(It.IsAny<IHasProviderIds>()))
                .Returns(Enumerable.Empty<ExternalIdInfo>());
            var config = new Mock<IConfigurationManager>();
            config.Setup(x => x.GetConfiguration(It.IsAny<string>()))
                .Returns(new XbmcMetadataOptions());
            _parser = new MovieNfoParser(new NullLogger<MovieNfoParser>(), config.Object, providerManager.Object);
        }

        [Fact]
        public void Fetch_Valid_Succes()
        {
            var result = new MetadataResult<Video>()
            {
                Item = new Video()
            };

            _parser.Fetch(result, "Test Data/Justice League.nfo", CancellationToken.None);
            var item = result.Item;

            Assert.Equal("Justice League", item.OriginalTitle);
            Assert.Equal("Justice for all.", item.Tagline);
            Assert.Equal("tt0974015", item.ProviderIds["imdb"]);

            Assert.Equal(4, item.Genres.Length);
            Assert.Contains("Action", item.Genres);
            Assert.Contains("Adventure", item.Genres);
            Assert.Contains("Fantasy", item.Genres);
            Assert.Contains("Sci-Fi", item.Genres);

            Assert.Equal(new DateTime(2017, 11, 15), item.PremiereDate);
            Assert.Single(item.Studios);
            Assert.Contains("DC Comics", item.Studios);

            Assert.Equal("1.777778", item.AspectRatio);
            Assert.Equal(1920, item.Width);
            Assert.Equal(1080, item.Height);
            Assert.Equal(new TimeSpan(0, 0, 6268).Ticks, item.RunTimeTicks);
            Assert.True(item.HasSubtitles);

            Assert.Equal(18, result.People.Count);

            var writers = result.People.Where(x => x.Type == PersonType.Writer).ToArray();
            Assert.Equal(2, writers.Length);
            var writerNames = writers.Select(x => x.Name);
            Assert.Contains("Jerry Siegel", writerNames);
            Assert.Contains("Joe Shuster", writerNames);

            var directors = result.People.Where(x => x.Type == PersonType.Director).ToArray();
            Assert.Single(directors);
            Assert.Equal("Zack Snyder", directors[0].Name);

            var actors = result.People.Where(x => x.Type == PersonType.Actor).ToArray();
            Assert.Equal(15, actors.Length);

            // Only test one actor
            var aquaman = actors.FirstOrDefault(x => x.Role.Equals("Aquaman", StringComparison.Ordinal));
            Assert.NotNull(aquaman);
            Assert.Equal("Jason Momoa", aquaman!.Name);
            Assert.Equal(5, aquaman!.SortOrder);
            Assert.Equal("https://m.media-amazon.com/images/M/MV5BMTI5MTU5NjM1MV5BMl5BanBnXkFtZTcwODc4MDk0Mw@@._V1_SX1024_SY1024_.jpg", aquaman!.ImageUrl);

            Assert.Equal(new DateTime(2019, 8, 6, 9, 1, 18), item.DateCreated);
        }

        [Fact]
        public void Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<Video>();

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, "Test Data/Justice League.nfo", CancellationToken.None));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<Video>()
            {
                Item = new Video()
            };

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, string.Empty, CancellationToken.None));
        }
    }
}
