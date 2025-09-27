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
        public void CleanName_keeps_punctuation_and_search_without_punctuation_fails()
        {
            var series = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Mr. Robot"
            };

       series.SortName = "Mr. Robot";

            var entity = _repo.Map(series);

            // Map sets CleanName using GetCleanValue (lowercases, removes diacritics but keeps punctuation)
            Assert.Equal("mr. robot", entity.CleanName);

            var searchTerm = "Mr Robot".ToLowerInvariant();

         Assert.Contains(searchTerm, entity.CleanName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
    }
}
