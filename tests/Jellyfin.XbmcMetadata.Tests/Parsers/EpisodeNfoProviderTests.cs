using System;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Movies;
using MediaBrowser.XbmcMetadata.Parsers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.XbmcMetadata.Tests.Parsers
{
    public class EpisodeNfoProviderTests
    {
        private readonly EpisodeNfoParser _parser;

        public EpisodeNfoProviderTests()
        {
            var providerManager = new Mock<IProviderManager>();

            var imdbExternalId = new ImdbExternalId();
            var externalIdInfo = new ExternalIdInfo(imdbExternalId.ProviderName, imdbExternalId.Key, imdbExternalId.Type, imdbExternalId.UrlFormatString);

            providerManager.Setup(x => x.GetExternalIdInfos(It.IsAny<IHasProviderIds>()))
                .Returns(new[] { externalIdInfo });

            var config = new Mock<IConfigurationManager>();
            config.Setup(x => x.GetConfiguration(It.IsAny<string>()))
                .Returns(new XbmcMetadataOptions());
            var user = new Mock<IUserManager>();
            var userData = new Mock<IUserDataManager>();
            var directoryService = new Mock<IDirectoryService>();

            _parser = new EpisodeNfoParser(
                new NullLogger<EpisodeNfoParser>(),
                config.Object,
                providerManager.Object,
                user.Object,
                userData.Object,
                directoryService.Object);
        }

        [Fact]
        public void Fetch_Valid_Success()
        {
            var result = new MetadataResult<Episode>()
            {
                Item = new Episode()
            };

            _parser.Fetch(result, "Test Data/The Bone Orchard.nfo", CancellationToken.None);

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
            Assert.Equal(1, item.IndexNumberEnd);
            Assert.Equal(2, item.AirsAfterSeasonNumber);
            Assert.Equal(3, item.AirsBeforeSeasonNumber);
            Assert.Equal(1, item.AirsBeforeEpisodeNumber);
            Assert.Equal("tt5017734", item.ProviderIds[MetadataProvider.Imdb.ToString()]);
            Assert.Equal("1276153", item.ProviderIds[MetadataProvider.Tmdb.ToString()]);

            // Credits
            var writers = result.People.Where(x => x.Type == PersonKind.Writer).ToArray();
            Assert.Equal(2, writers.Length);
            Assert.Contains("Bryan Fuller", writers.Select(x => x.Name));
            Assert.Contains("Michael Green", writers.Select(x => x.Name));

            // Directors
            var directors = result.People.Where(x => x.Type == PersonKind.Director).ToArray();
            Assert.Single(directors);
            Assert.Contains("David Slade", directors.Select(x => x.Name));

            // Actors
            var actors = result.People.Where(x => x.Type == PersonKind.Actor).ToArray();
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
        public void Fetch_Valid_MultiEpisode_Success()
        {
            var result = new MetadataResult<Episode>()
            {
                Item = new Episode()
            };

            _parser.Fetch(result, "Test Data/Rising.nfo", CancellationToken.None);

            var item = result.Item;
            Assert.Equal("Rising (1) / Rising (2)", item.Name);
            Assert.Equal(1, item.IndexNumber);
            Assert.Equal(2, item.IndexNumberEnd);
            Assert.Equal(1, item.ParentIndexNumber);
            Assert.Equal("A new Stargate team embarks on a dangerous mission to a distant galaxy, where they discover a mythical lost city -- and a deadly new enemy. / Sheppard tries to convince Weir to mount a rescue mission to free Colonel Sumner, Teyla, and the others captured by the Wraith.", item.Overview);
            Assert.Equal(new DateTime(2004, 7, 16), item.PremiereDate);
            Assert.Equal(2004, item.ProductionYear);
        }

        [Fact]
        public void Fetch_Valid_MultiEpisode_With_Missing_Tags_Success()
        {
            var result = new MetadataResult<Episode>()
            {
                Item = new Episode()
            };

            _parser.Fetch(result, "Test Data/Stargate Atlantis S01E01-E04.nfo", CancellationToken.None);

            var item = result.Item;
            // <title> provided for episode 1, 3 and 4
            Assert.Equal("Rising / Hide and Seek / Thirty-Eight Minutes", item.Name);
            // <originaltitle> provided for all episodes
            Assert.Equal("Rising (1) / Rising (2) / Hide and Seek / Thirty-Eight Minutes", item.OriginalTitle);
            Assert.Equal(1, item.IndexNumber);
            Assert.Equal(4, item.IndexNumberEnd);
            Assert.Equal(1, item.ParentIndexNumber);
            // <plot> only provided for episode 1
            Assert.Equal("A new Stargate team embarks on a dangerous mission to a distant galaxy, where they discover a mythical lost city -- and a deadly new enemy.", item.Overview);
            Assert.Equal(new DateTime(2004, 7, 16), item.PremiereDate);
            Assert.Equal(2004, item.ProductionYear);
        }

        [Fact]
        public void Parse_GivenFileWithThumbWithoutAspect_Success()
        {
            var result = new MetadataResult<Episode>
            {
                Item = new Episode()
            };

            _parser.Fetch(result, "Test Data/Sonarr-Thumb.nfo", CancellationToken.None);

            Assert.Single(result.RemoteImages, x => x.Type == ImageType.Primary);
            Assert.Equal("https://artworks.thetvdb.com/banners/episodes/359095/7081317.jpg", result.RemoteImages.First(x => x.Type == ImageType.Primary).Url);
        }

        [Fact]
        public void Fetch_WithNullItem_ThrowsArgumentException()
        {
            var result = new MetadataResult<Episode>();

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, "Test Data/The Bone Orchard.nfo", CancellationToken.None));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<Episode>()
            {
                Item = new Episode()
            };

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, string.Empty, CancellationToken.None));
        }
    }
}
