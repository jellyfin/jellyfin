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
using MediaBrowser.XbmcMetadata.Parsers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.XbmcMetadata.Tests.Parsers
{
    public class SeasonNfoProviderTests
    {
        private readonly SeasonNfoParser _parser;

        public SeasonNfoProviderTests()
        {
            var providerManager = new Mock<IProviderManager>();
            providerManager.Setup(x => x.GetExternalIdInfos(It.IsAny<IHasProviderIds>()))
                .Returns(Enumerable.Empty<ExternalIdInfo>());
            var config = new Mock<IConfigurationManager>();
            config.Setup(x => x.GetConfiguration(It.IsAny<string>()))
                .Returns(new XbmcMetadataOptions());
            var user = new Mock<IUserManager>();
            var userData = new Mock<IUserDataManager>();
            var directoryService = new Mock<IDirectoryService>();

            _parser = new SeasonNfoParser(
                new NullLogger<SeasonNfoParser>(),
                config.Object,
                providerManager.Object,
                user.Object,
                userData.Object,
                directoryService.Object);
        }

        [Fact]
        public void Fetch_Valid_Success()
        {
            var result = new MetadataResult<Season>()
            {
                Item = new Season()
            };

            _parser.Fetch(result, "Test Data/Season 01.nfo", CancellationToken.None);
            var item = result.Item;

            Assert.Equal("Season 1", item.Name);
            Assert.Equal(1, item.IndexNumber);
            Assert.False(item.IsLocked);
            Assert.Equal(2019, item.ProductionYear);
            Assert.Equal(new DateTime(2019, 11, 08), item.PremiereDate);
            Assert.Equal(new DateTime(2020, 06, 14, 17, 26, 51), item.DateCreated);

            Assert.Equal(10, result.People.Count);

            Assert.True(result.People.All(x => x.Type == PersonKind.Actor));

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

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, "Test Data/Season 01.nfo", CancellationToken.None));
        }

        [Fact]
        public void Fetch_NullResult_ThrowsArgumentException()
        {
            var result = new MetadataResult<Season>()
            {
                Item = new Season()
            };

            Assert.Throws<ArgumentException>(() => _parser.Fetch(result, string.Empty, CancellationToken.None));
        }
    }
}
