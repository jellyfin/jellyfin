using System;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data
{
    public class SearchPunctuationTests
    {
        private readonly IFixture _fixture;
        private readonly BaseItemRepository _repo;

        public SearchPunctuationTests()
        {
            var appHost = new Mock<MediaBrowser.Controller.IServerApplicationHost>();
            appHost.Setup(x => x.ExpandVirtualPath(It.IsAny<string>()))
                .Returns((string x) => x);
            appHost.Setup(x => x.ReverseVirtualPath(It.IsAny<string>()))
                .Returns((string x) => x);

            var configSection = new Mock<IConfigurationSection>();
            configSection
                .SetupGet(x => x[It.Is<string>(s => s == MediaBrowser.Controller.Extensions.ConfigurationExtensions.SqliteCacheSizeKey)])
                .Returns("0");
            var config = new Mock<IConfiguration>();
            config
                .Setup(x => x.GetSection(It.Is<string>(s => s == MediaBrowser.Controller.Extensions.ConfigurationExtensions.SqliteCacheSizeKey)))
                .Returns(configSection.Object);

            _fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            _fixture.Inject(appHost.Object);
            _fixture.Inject(config.Object);

            _repo = _fixture.Create<BaseItemRepository>();
        }

        [Fact]
        public void CleanName_keeps_punctuation_and_search_without_punctuation_passes()
        {
            var series = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Mr. Robot"
            };

            series.SortName = "Mr. Robot";

            var entity = _repo.Map(series);
            Assert.Equal("mr robot", entity.CleanName);

            var searchTerm = "Mr Robot".ToLowerInvariant();

            Assert.Contains(searchTerm, entity.CleanName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("Spider-Man: Homecoming", "spider man homecoming")]
        [InlineData("Beyoncé — Live!", "beyonce live")]
        [InlineData("Hello, World!", "hello world")]
        [InlineData("(The) Good, the Bad & the Ugly", "the good the bad the ugly")]
        [InlineData("Wall-E", "wall e")]
        [InlineData("No. 1: The Beginning", "no 1 the beginning")]
        [InlineData("Café-au-lait", "cafe au lait")]
        public void CleanName_normalizes_various_punctuation(string title, string expectedClean)
        {
            var series = new Series
            {
                Id = Guid.NewGuid(),
                Name = title
            };

            series.SortName = title;

            var entity = _repo.Map(series);

            Assert.Equal(expectedClean, entity.CleanName);

            // Ensure a search term without punctuation would match
            var searchTerm = expectedClean;
            Assert.Contains(searchTerm, entity.CleanName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("Face/Off", "face off")]
        [InlineData("V/H/S", "v h s")]
        public void CleanName_normalizes_titles_withslashes(string title, string expectedClean)
        {
            var series = new Series
            {
                Id = Guid.NewGuid(),
                Name = title
            };

            series.SortName = title;

            var entity = _repo.Map(series);

            Assert.Equal(expectedClean, entity.CleanName);

            // Ensure a search term without punctuation would match
            var searchTerm = expectedClean;
            Assert.Contains(searchTerm, entity.CleanName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
