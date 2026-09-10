using System;
using System.IO;
using System.Linq;
using System.Threading;
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
    public class SeriesNfoSeasonParserTests
    {
        private readonly SeriesNfoSeasonParser _parser;

        public SeriesNfoSeasonParserTests()
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

            _parser = new SeriesNfoSeasonParser(
                new NullLogger<SeriesNfoSeasonParser>(),
                config.Object,
                providerManager.Object,
                user.Object,
                userData.Object,
                directoryService.Object);
        }

        [Fact]
        public void Fetch_WithSeasonPosterInTvShowNfo_AddsRemoteImage()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8" standalone="yes"?>
                <tvshow>
                    <title>Test Series</title>
                    <namedseason number="1">Season 1 Name</namedseason>
                    <thumb season="1" type="season" aspect="poster">https://image.tmdb.org/t/p/original/7u443QI5xNIfLgNzEsV43CYZCWX.jpg</thumb>
                    <thumb season="2" type="season" aspect="poster">https://image.tmdb.org/t/p/original/other.jpg</thumb>
                </tvshow>
                """;

            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, xml);

                var season1 = new Season { IndexNumber = 1 };
                var result = new MetadataResult<Season> { Item = season1 };

                _parser.Fetch(result, tempFile, CancellationToken.None);

                Assert.Equal("Season 1 Name", result.Item.Name);
                Assert.Single(result.RemoteImages);
                Assert.Equal("https://image.tmdb.org/t/p/original/7u443QI5xNIfLgNzEsV43CYZCWX.jpg", result.RemoteImages[0].Url);
                Assert.Equal(ImageType.Primary, result.RemoteImages[0].Type);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void Fetch_WithSeriesLevelOrInvalidThumb_IgnoresForSeason()
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8" standalone="yes"?>
                <tvshow>
                    <title>Test Series</title>
                    <thumb aspect="poster">https://image.tmdb.org/t/p/original/series-poster.jpg</thumb>
                    <thumb season="invalid" aspect="poster">https://image.tmdb.org/t/p/original/invalid.jpg</thumb>
                    <thumb season="2" aspect="poster">https://image.tmdb.org/t/p/original/season2.jpg</thumb>
                </tvshow>
                """;

            var tempFile = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempFile, xml);

                var season1 = new Season { IndexNumber = 1 };
                var result = new MetadataResult<Season> { Item = season1 };

                _parser.Fetch(result, tempFile, CancellationToken.None);

                Assert.Empty(result.RemoteImages);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
